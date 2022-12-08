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
    [DataContract]
    public class MediaData : INotifyPropertyChanged
    {
        [DataMember]
        public Guid ID { get; set; } = Guid.NewGuid();

        [DataMember]
        public bool HasEverGeneratedMetaData { get; set; } = false;

        [DataMember]
        public Dictionary<string, bool> GeneratedMetaData { get; set; } = new Dictionary<string, bool>();

        [DataMember]
        public string LocalFilePath { get; set; } = "";

        [DataMember]
        public string Title { get; set; } = "";

        [DataMember]
        public string Description { get; set; } = "";

        [DataMember]
        public string ReleaseDate { get; set; } = "";

        [DataMember]
        public string MPAARating { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;

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

        private string _posterImagePath = "";
        [DataMember]
        public string PosterImagePath { get { return _posterImagePath; } set { _posterImagePath = value; NotifyPropertyChanged(nameof(PosterImage)); } }

        private string _backdropImagePath = "";
        [DataMember]
        public string BackdropImagePath { get { return _backdropImagePath; } set { _backdropImagePath = value; NotifyPropertyChanged(nameof(BackdropImage)); } }

        public void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        virtual public bool IsAllMetaDataGenerated
        {
            get
            {
                return
                    GeneratedMetaData.GetValueOrDefault(nameof(Title)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(MPAARating)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(Description)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(ReleaseDate)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(PosterImagePath)) &&
                    GeneratedMetaData.GetValueOrDefault(nameof(BackdropImagePath));
            }
        }

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
}
