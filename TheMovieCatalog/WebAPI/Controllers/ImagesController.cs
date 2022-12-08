
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheMovieCatalog.DataModels;
using TheMovieCatalog.Shared;
using TheMovieCatalog.Shared.ExtensionMethods;

namespace TheMovieCatalog.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    public class ImagesController : Controller
    {
        [HttpGet]
        [Route("{MediaID}")]
        [Produces("application/octet-stream")]
        async public Task<Stream?> Poster(string MediaID = "")
        {
            var mediaID = Guid.Parse(MediaID);
            var movie = await App.Current.Dispatcher.InvokeAsync(() =>
            {
                if (MainWindow.Instance.Libraries.IsNullOrEmpty())
                    return null;
                else
                {
                    var movieLibraries = MainWindow.Instance.Libraries.Where((l) => l.MediaLibraryType == MediaLibraryType.Movies);
                    if (movieLibraries.IsNullOrEmpty())
                        return null;
                    else
                    {
                        foreach (var movieLibrary in movieLibraries)
                        {
                            var movie = movieLibrary.MediaMetaDatas.Find((m) => ((MovieData)m).ID == mediaID) as MovieData;
                            if (movie != null)
                                return movie;
                        }
                    }
                    return null;
                }
            });

            if (movie == null)
                return null;

            return movie.PosterImage?.StreamSource;
        }

        [HttpGet]
        [Route("{MediaID}")]
        [Produces("application/octet-stream")]
        async public Task<Stream?> Backdrop(string MediaID = "")
        {
            var mediaID = Guid.Parse(MediaID);
            var movie = await App.Current.Dispatcher.InvokeAsync(() =>
            {
                if (MainWindow.Instance.Libraries.IsNullOrEmpty())
                    return null;
                else
                {
                    var movieLibraries = MainWindow.Instance.Libraries.Where((l) => l.MediaLibraryType == MediaLibraryType.Movies);
                    if (movieLibraries.IsNullOrEmpty())
                        return null;
                    else
                    {
                        foreach (var movieLibrary in movieLibraries)
                        {
                            var movie = movieLibrary.MediaMetaDatas.Find((m) => ((MovieData)m).ID == mediaID) as MovieData;
                            if (movie != null)
                                return movie;
                        }
                    }
                    return null;
                }
            });

            if (movie == null)
                return null;

            return movie.BackdropImage?.StreamSource;
        }
    }
}
