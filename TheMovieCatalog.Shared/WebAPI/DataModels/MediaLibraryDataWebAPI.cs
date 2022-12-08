using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TheMovieCatalog.Shared;

namespace TheMovieCatalog.WebAPI.DataModels
{
    public class MoviesLibraryDataWebAPI
    {
        public void InitMoviesLibrary(MediaLibraryType mediaLibraryType, List<MovieDataWebAPI> movieMetaDatas, string title, string mediaFolder)
        {
            this.MovieMetaDatas = movieMetaDatas;
            this.Title = title;
            this.MediaFolder = mediaFolder;
        }

        public List<MovieDataWebAPI> MovieMetaDatas { get; set; }

        public string Title { get; set; }

        public string MediaFolder { get; set; }
    }
}
