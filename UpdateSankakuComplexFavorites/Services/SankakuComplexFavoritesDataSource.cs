using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MithrasSoftware.UpdateSankakuComplexFavorites.Services
{
    public class SankakuComplexFavoritesDataSource
    {
        private readonly string _fileName;


        public SankakuComplexFavoritesDataSource(string fileName)
        {
            _fileName = fileName;
        }


        public HashSet<Guid> GetFavorites()
        {
            var value = File.Exists(_fileName)
                ? File.ReadAllText(_fileName)
                : null;
            return value != null
                ? JsonConvert.DeserializeObject<HashSet<Guid>>(value)
                : new HashSet<Guid>();
        }
        public void SetFavorites(HashSet<Guid> favorites)
        {
            var value = JsonConvert.SerializeObject(favorites);
            File.WriteAllText(_fileName, value);
        }
    }
}