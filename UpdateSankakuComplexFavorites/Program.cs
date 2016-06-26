using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MithrasSoft.UpdateSankakuComplexFavorites.ServiceAgents;
using MithrasSoft.UpdateSankakuComplexFavorites.Services;

namespace MithrasSoft.UpdateSankakuComplexFavorites
{
    class Program
    {
        private const int DegreeOfParallelism = 4;


        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var mainTask = MainAsync(args, cts.Token);
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
            Console.WriteLine("Waiting for pending operations to complete...");
            cts.Cancel();
            mainTask.GetAwaiter().GetResult();
        }


        private static async Task MainAsync(string[] args, CancellationToken cancellationToken)
        {
            var login = args[0];
            var password = args[1];
            var imagePath = args[2];
            var favoritesFileName = args[3];

            var iqdbServiceAgent = new IQDBServiceAgent();
            var sankakucomplexServiceAgent = new SankakucomplexServiceAgent();
            await sankakucomplexServiceAgent.Login(login, password);
            var sankakuComplexService = new SankakuComplexService(iqdbServiceAgent, sankakucomplexServiceAgent);
            var favoritesDataSource = new SankakuComplexFavoritesDataSource(favoritesFileName);
            var favorites = favoritesDataSource.GetFavorites();

            var fileNames = new List<string>();
            fileNames.AddRange(Directory.GetFiles(imagePath, "*.jpg", SearchOption.AllDirectories));
            fileNames.AddRange(Directory.GetFiles(imagePath, "*.png", SearchOption.AllDirectories));
            fileNames.AddRange(Directory.GetFiles(imagePath, "*.webp", SearchOption.AllDirectories));
            fileNames.AddRange(Directory.GetFiles(imagePath, "*.gif", SearchOption.AllDirectories));
            fileNames = fileNames.Where(fileName =>
            {
                Guid guid;
                var name = fileName.Substring(fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                name = name.Substring(0, name.LastIndexOf('.'));
                return name.Length == 32
                    && Guid.TryParse(name.Substring(0, 32), out guid)
                    && !favorites.Contains(guid);
            }).ToList();

            var semaphore = new SemaphoreSlim(DegreeOfParallelism);
            for (var i = 0; i < fileNames.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var fileName = fileNames[i];
                await semaphore.WaitAsync();
                Console.WriteLine($"Processing file {i}/{fileNames.Count}...");
#pragma warning disable 4014
                sankakuComplexService.AddToFavorites(fileName)
                    .ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            Console.WriteLine(task.Exception.InnerException.Message);
                        }
                        else
                        {
                            var name = fileName.Substring(fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                            name = name.Substring(0, name.LastIndexOf('.'));
                            favorites.Add(Guid.Parse(name));
                        }
                    })
                    .ContinueWith(task =>
                    {
                        semaphore.Release();
                    });
#pragma warning restore 4014
            }
            // wait for all the tasks to complete
            for (var i = 0; i < DegreeOfParallelism - 1; i++) { await semaphore.WaitAsync(); }
            favoritesDataSource.SetFavorites(favorites);

            Console.WriteLine("Done.");
        }
    }
}
