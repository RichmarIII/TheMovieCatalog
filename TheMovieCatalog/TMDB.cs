using System;
using System.IO;

namespace TheMovieCatalog
{
    internal class TMDB
    {
        private static string APIKeyFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheMovieCatalog", "TMDB_API_KEY.txt");
        public static string APIKey => File.ReadAllText(APIKeyFilePath);
    }
}
