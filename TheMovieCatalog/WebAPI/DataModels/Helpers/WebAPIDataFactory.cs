using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheMovieCatalog.DataModels;
using TheMovieCatalog.Shared;

namespace TheMovieCatalog.WebAPI.DataModels.Helpers
{
    public class WebAPIDataFactory
    {
        static public MoviesLibraryDataWebAPI CreateMoviesLibraryDataWebAPI(MediaLibraryData mediaLibrary)
        {
            if (mediaLibrary.MediaLibraryType != MediaLibraryType.Movies)
                return null;

            var mediaLibraryWebAPI = new MoviesLibraryDataWebAPI();

            List<MovieDataWebAPI>? mediaMetaDatas = null;

            mediaMetaDatas = mediaLibrary.MediaMetaDatas.Select(mediaMetaData =>
            {
                var movieMetaData = (MovieData)mediaMetaData;
                var movieWebAPI = CreateMovieDataWebAPI(movieMetaData);
                return movieWebAPI;
            }).ToList();

            mediaLibraryWebAPI.InitMoviesLibrary(mediaLibrary.MediaLibraryType, mediaMetaDatas, mediaLibrary.Title, mediaLibrary.MediaFolder.FullName);

            return mediaLibraryWebAPI;
        }

        static public MediaDataWebAPI CreateMediaDataWebAPI(MediaData media)
        {
            var mediaWebAPI = new MediaDataWebAPI();
            mediaWebAPI.InitMedia(media.Description, media.ID, media.LocalFilePath, media.MPAARating, media.ReleaseDate, media.Title);
            return mediaWebAPI;
        }

        static public MovieDataWebAPI CreateMovieDataWebAPI(MovieData movie)
        {
            var movieWebAPI = new MovieDataWebAPI();
            movieWebAPI.InitMovie(movie.Description, movie.ID, movie.LocalFilePath, movie.MPAARating, movie.ReleaseDate, movie.Title);
            return movieWebAPI;
        }
    }
}
