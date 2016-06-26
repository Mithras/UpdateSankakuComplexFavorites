using System;
using System.IO;
using System.Threading.Tasks;
using MithrasSoft.UpdateSankakuComplexFavorites.ServiceAgents;

namespace MithrasSoft.UpdateSankakuComplexFavorites.Services
{
    public class SankakuComplexService
    {
        private readonly IQDBServiceAgent _iqdbServiceAgent;
        private readonly SankakucomplexServiceAgent _sankakucomplexServiceAgent;


        public SankakuComplexService(IQDBServiceAgent iqdbServiceAgent, SankakucomplexServiceAgent sankakucomplexServiceAgent)
        {
            _iqdbServiceAgent = iqdbServiceAgent;
            _sankakucomplexServiceAgent = sankakucomplexServiceAgent;
        }


        public async Task AddToFavorites(string fileName)
        {
            var matches = await _iqdbServiceAgent.FindMatches(fileName, new[] { IQDBServiceAgent.ServiceEnum.SankakuChannel });

            foreach (var match in matches)
            {
                var name = fileName.Substring(fileName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                name = name.Substring(0, name.LastIndexOf('.'));
                if (match.Name == name)
                {
                    await _sankakucomplexServiceAgent.AddToFavorites(match.Id);
                    return;
                }
            }
            throw new Exception($"No match for {fileName}");
        }
    }
}