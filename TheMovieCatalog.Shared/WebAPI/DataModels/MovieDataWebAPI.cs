using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMovieCatalog.WebAPI.DataModels
{
    public class MovieDataWebAPI
    {
        public void InitMovie(string description, Guid id, string remoteFilePath, string certification, string releaseDate, string title)
        {
            this.Description = description;
            this.ID = id;
            this.RemoteFilePath = remoteFilePath;
            this.Certification = certification;
            this.ReleaseDate = releaseDate;
            this.Title = title;
        }

        public string Description;
        public Guid ID;
        public string RemoteFilePath;
        public string Certification;
        public string ReleaseDate;
        public string Title;
    }
}
