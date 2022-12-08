using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TheMovieCatalog.WebAPI.DataModels;

namespace TheMovieCatalog.Shared
{
    public static class HelperMethods
    {
        async public static Task DownloadFileAsync(Uri uri, string filePath)
        {
            try
            {
                HttpClient httpClient = new();
                using (var netStream = await httpClient.GetStreamAsync(uri))
                {
                    using (var fileStream = File.Create(filePath))
                    {
                        await netStream.CopyToAsync(fileStream);
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public static string GetMediaServerURL()
        {
            return @"http://10.0.0.38:1234";
        }

        async public static Task<Stream> GetMoviePosterAsync(MovieDataWebAPI movie)
        {
            try
            {
                HttpClient httpClient = new();
                using (var netStream = await httpClient.GetStreamAsync(new Uri(String.Format(@"{0}/api/Images/Poster/{1}", GetMediaServerURL(), movie.ID.ToString()))))
                {
                    using Stream stream = new MemoryStream();
                    await netStream.CopyToAsync(stream);
                    return stream;
                }
            }
            catch
            {
                throw;
            }
        }
        
        async public static Task<Stream> GetMovieBackdropAsync(MovieDataWebAPI movie)
        {
            try
            {
                HttpClient httpClient = new();
                using (var netStream = await httpClient.GetStreamAsync(new Uri(String.Format(@"{0}/api/Images/Backdrop/{1}", GetMediaServerURL(), movie.ID.ToString()))))
                {
                    using Stream stream = new MemoryStream();
                    await netStream.CopyToAsync(stream);
                    return stream;
                }
            }
            catch
            {
                throw;
            }
        }

        async public static Task<List<MoviesLibraryDataWebAPI>?> GetMovieLibrariesAsync()
        {
            try
            {
                HttpClient httpClient = new();
                var jsonLibraries = await httpClient.GetStringAsync(new Uri(String.Format("{0}/api/Libraries/Movies", GetMediaServerURL())));
                var Libraries = JsonConvert.DeserializeObject<List<MoviesLibraryDataWebAPI>>(jsonLibraries);
                return Libraries;
            }
            catch
            {

                throw;
            }
        }

        async public static Task<Stream> GetMovieStreamAsync(MovieDataWebAPI movie)
        {
            try
            {
                HttpClient httpClient = new();
                return await httpClient.GetStreamAsync(new Uri(String.Format("{0}/api/Movies/Play/{1}", GetMediaServerURL(), movie.ID.ToString())));
            }
            catch
            {
                throw;
            }
        }
    }
}
