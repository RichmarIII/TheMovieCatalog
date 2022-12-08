using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMovieCatalog.WebAPI.DataModels
{
    public class MediaDataWebAPI
    {
        public void InitMedia(string description, Guid id, string remoteFilePath, string certification, string releaseDate, string title)
        {
            this.Description = description;
            this.ID = id;
            this.RemoteFilePath = remoteFilePath;
            this.Certification = certification;
            this.ReleaseDate = releaseDate;
            this.Title = title;
        }

        public string Description { get; private set; }
        public Guid ID { get; private set; }
        public string RemoteFilePath { get; private set; }
        public string Certification { get; private set; }
        public string ReleaseDate { get; private set; }
        public string Title { get; private set; }
    }
}
