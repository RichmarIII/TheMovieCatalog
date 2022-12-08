using LibVLCSharp.Shared;
using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TheMovieCatalog.DataModels;
using TheMovieCatalog.Shared;
using TheMovieCatalog.Shared.ExtensionMethods;

namespace TheMovieCatalog.ViewModels
{
    /// <summary>
    /// Interaction logic for TheaterPageView.xaml
    /// </summary>
    public partial class TheaterPageView : Page
    {
        private LibVLC? _libVLC;
        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
        private Pipe pipe = new Pipe();
        private MediaData? _media;

        static private TheaterPageView? _instance;
        static public TheaterPageView Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TheaterPageView();
                }
                return _instance;
            }
        }

        public TheaterPageView()
        {
            InitializeComponent();

            Core.Initialize();

            _libVLC = new LibVLC();
            _libVLC.Log += LibVLC_Log;
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _videoView.MediaPlayer = _mediaPlayer;
            _videoControls.SetMediaPlayer(_mediaPlayer);
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
        }

        private void StopMedia()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Pause();
                if (MainWindow.Instance.Frame.CanGoBack)
                    MainWindow.Instance.Frame.GoBack();
            }
        }

        public void PlayMedia(MediaData? media)
        {
            MainWindow.Instance.Frame.Navigate(this);
            
            if (_mediaPlayer != null && _libVLC != null && media != null)
            {
                _media = media;
                _mediaPlayer.SetMarqueeString(VideoMarqueeOption.Text, media.Title);
                _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Timeout, 5000);
                _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Enable, 1);
                

                Task.Run(() =>
                {
                    bool isLoaded = false;
                    while (!isLoaded)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            isLoaded = IsLoaded;
                        });
                    }
                    using var vlcMedia = new Media(_libVLC, _media.LocalFilePath);
                    _mediaPlayer.Play(vlcMedia);
                });
            }
        }

        private void VideoView_Loaded(object sender, RoutedEventArgs e)
        {

            //Task.Run(async () =>
            //{
            //    var client = new HttpClient();
            //    Stream? stream = await client.GetStreamAsync("http://localhost:1234/api/libraries/video2");
            //    try
            //    {
            //        byte[] buff = new byte[1024 * 4];
            //        var len = 0;
            //        while ((len = await stream.ReadAsync(buff)) > 0)
            //        {
            //            await pipe.Writer.WriteAsync(buff.AsMemory(0, len));
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Debug.WriteLine(ex);
            //    }
            //});
        }

        private void LibVLC_Log(object? sender, LogEventArgs e)
        {
            Debug.WriteLine(e.Message);
        }

        private void ExitMovieButton_Tapped(object sender, RoutedEventArgs e)
        {
            StopMedia();
        }
        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                bool bHasNextMedia = false;
                if (_media != null)
                {
                    if (_media is TVEpisodeData tvEpisode)
                    {
                        var TVShowLibraries = MainWindow.Instance.Libraries.Where(l => l.MediaLibraryType == MediaLibraryType.TVShows).ToList();
                        if (!TVShowLibraries.IsNullOrEmpty())
                        {
                            var tvSeries = TVShowLibraries.Select(l => (TVSeriesData?)l.MediaMetaDatas.Find(ser => ((TVSeriesData)ser).ID == tvEpisode.SeriesID));
                            if (!tvSeries.IsNullOrEmpty())
                            {
                                var tvSeasons = tvSeries.Select(ser => ser.Seasons.Find(sea => sea.ID == tvEpisode.SeasonID));
                                if (!tvSeasons.IsNullOrEmpty())
                                {
                                    var nextEpisode = tvSeasons.First()?.Episodes.Find(ep => ep.EpisodeNumber == (tvEpisode.EpisodeNumber + 1));
                                    if (nextEpisode != null)
                                    {
                                        PlayMedia(nextEpisode);
                                        bHasNextMedia = true;
                                    }
                                    else
                                    {
                                        tvSeasons = tvSeries.Select(ser => ser.Seasons.Find(sea => sea.SeasonNumber == tvEpisode.SeasonNumber + 1));
                                        if (!tvSeasons.IsNullOrEmpty())
                                        {
                                            var season = tvSeasons.First();
                                            if (season != null)
                                            {
                                                nextEpisode = season.Episodes.First();
                                                if (nextEpisode != null)
                                                {
                                                    PlayMedia(nextEpisode);
                                                    bHasNextMedia = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!bHasNextMedia)
                {
                    StopMedia();
                }
            });

        }

        private void _videoView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _videoControls.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }

    }
}
