using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TheMovieCatalog.Shared.ExtensionMethods;

namespace TheMovieCatalog.DataModels
{
    public class TVSeriesData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        [DataMember]
        public bool HasEverGeneratedMetaData { get; set; } = false;

        [DataMember]
        public Dictionary<string, bool> GeneratedMetaData { get; set; } = new Dictionary<string, bool>();

        [DataMember]
        public Guid ID { get; set; } = Guid.NewGuid();

        [DataMember]
        public string Title { get; set; } = "";

        [DataMember]
        public string Description { get; set; } = "";

        [DataMember]
        public string ReleaseDateStart { get; set; } = "";

        [DataMember]
        public string ReleaseDateEnd { get; set; } = "";

        private BitmapImage? _posterImage = null;
        public BitmapImage? PosterImage
        {
            get
            {
                if (_posterImage == null && !PosterImagePath.IsNullOrEmpty())
                {
                    LoadPosterImageAsync();
                }
                return _posterImage;
            }
        }

        private string _posterImagePath = "";

        [DataMember]
        public string PosterImagePath { get { return _posterImagePath; } set { _posterImagePath = value; NotifyPropertyChanged(nameof(PosterImage)); } }

        private BitmapImage? _backdropImage = null;
        public BitmapImage? BackdropImage
        {
            get
            {
                if (_backdropImage == null && !BackdropImagePath.IsNullOrEmpty())
                {
                    LoadBackdropImageAsync();
                }
                return _backdropImage;
            }
        }
        private string _backdropImagePath = "";
        public string BackdropImagePath { get { return _backdropImagePath; } set { _backdropImagePath = value; NotifyPropertyChanged(nameof(BackdropImage)); } }

        private List<TVSeasonData> _seasons = new List<TVSeasonData>();

        [DataMember]
        public List<TVSeasonData> Seasons { get { return _seasons; } set { _seasons = value; NotifyPropertyChanged(); } }

        private async Task LoadBackdropImageAsync()
        {
            BitmapImage? image = null;
            await Task.Run(() =>
            {
                try
                {
                    image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.None;
                    image.UriSource = new Uri(BackdropImagePath);
                    image.EndInit();
                    image.Freeze();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });

            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                _backdropImage = image;
                NotifyPropertyChanged(nameof(BackdropImage));
            });
        }

        private async Task LoadPosterImageAsync()
        {
            BitmapImage? image = null;
            await Task.Run(() =>
            {
                try
                {
                    image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.None;
                    image.UriSource = new Uri(PosterImagePath);
                    image.EndInit();
                    image.Freeze();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });

            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                _posterImage = image;
                NotifyPropertyChanged(nameof(PosterImage));
            });
        }
    }

    public class TVSeasonData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        [DataMember]
        public bool HasEverGeneratedMetaData { get; set; } = false;

        [DataMember]
        public Dictionary<string, bool> GeneratedMetaData { get; set; } = new Dictionary<string, bool>();

        [DataMember]
        public Guid ID { get; set; } = Guid.NewGuid();

        [DataMember]
        public Guid SeriesID { get; set; }

        [DataMember]
        public string Title { get; set; } = "";
        [DataMember]
        public string Description { get; set; } = "";
        [DataMember]
        public string ReleaseDate { get; set; } = "";
        [DataMember]
        public Int32 SeasonNumber { get; set; } = -1;

        private BitmapImage? _posterImage = null;
        public BitmapImage? PosterImage
        {
            get
            {
                if (_posterImage == null && !PosterImagePath.IsNullOrEmpty())
                {
                    LoadPosterImageAsync();
                }
                return _posterImage;
            }
        }

        private string _posterImagePath = "";

        [DataMember]
        public string PosterImagePath { get { return _posterImagePath; } set { _posterImagePath = value; NotifyPropertyChanged(nameof(PosterImage)); } }

        private BitmapImage? _backdropImage = null;
        public BitmapImage? BackdropImage
        {
            get
            {
                if (_backdropImage == null && !BackdropImagePath.IsNullOrEmpty())
                {
                    LoadBackdropImageAsync();
                }
                return _backdropImage;
            }
        }
        private string _backdropImagePath = "";
        public string BackdropImagePath { get { return _backdropImagePath; } set { _backdropImagePath = value; NotifyPropertyChanged(nameof(BackdropImage)); } }

        private List<TVEpisodeData> _episodes = new();

        [DataMember]
        public List<TVEpisodeData> Episodes { get { return _episodes; } set { _episodes = value; NotifyPropertyChanged(); } }

        private async Task LoadBackdropImageAsync()
        {
            BitmapImage? image = null;
            await Task.Run(() =>
            {
                try
                {
                    image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.None;
                    image.UriSource = new Uri(BackdropImagePath);
                    image.EndInit();
                    image.Freeze();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });

            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                _backdropImage = image;
                NotifyPropertyChanged(nameof(BackdropImage));
            });
        }

        private async Task LoadPosterImageAsync()
        {
            BitmapImage? image = null;
            await Task.Run(() =>
            {
                try
                {
                    image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.None;
                    image.UriSource = new Uri(PosterImagePath);
                    image.EndInit();
                    image.Freeze();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });

            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                _posterImage = image;
                NotifyPropertyChanged(nameof(PosterImage));
            });
        }
    }

    [DataContract]
    public class TVEpisodeData : MediaData
    {
        [DataMember]
        public Guid SeasonID { get; set; }

        [DataMember]
        public Guid SeriesID { get; set; }

        [DataMember]
        public string SeriesName { get; set; } = "";

        [DataMember]
        public Int32 SeriesStartYear { get; set; } = 1900;

        [DataMember]
        public Int32 SeasonNumber { get; set; } = -1;

        [DataMember]
        public Int32 EpisodeNumber { get; set; } = -1;
        override public bool IsAllMetaDataGenerated { get
            {
                return GeneratedMetaData.GetValueOrDefault(nameof(SeriesName)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(SeriesStartYear)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(SeasonNumber)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(EpisodeNumber)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(Title)) &&
                    //GeneratedMetaData.GetValueOrDefault(nameof(MPAARating)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(Description)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(ReleaseDate)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(PosterImagePath)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(BackdropImagePath));
            } }
    }
}