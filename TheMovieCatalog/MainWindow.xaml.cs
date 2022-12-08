using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using TheMovieCatalog.DataModels;
using TheMovieCatalog.ExtensionMethods;
using TheMovieCatalog.Shared;
using TheMovieCatalog.Shared.ExtensionMethods;
using TheMovieCatalog.ViewModels;
using TMDbLib.Client;
using TMDbLib.Objects.General;

namespace TheMovieCatalog
{
    /// <summary>
    /// A media input that allows you to push data to the video player as they are available, thanks to System.IO.Pipelines
    /// </summary>
    public class PipeMediaInput : MediaInput
    {
        private readonly PipeReader _reader;
        private bool _completed = true;// True until the media input has been opened by libvlc.

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="reader">The reader end of the pipe</param>
        public PipeMediaInput(PipeReader reader)
        {
            this._reader = reader;
            this.CanSeek = false;
        }

        /// <summary>
        /// Called by libvlc when the stream is closed
        /// </summary>
        public override void Close()
        {
            this._reader.Complete();
            this._reader.CancelPendingRead();
        }

        /// <summary>
        /// Called by libvlc when the stream opens
        /// </summary>
        /// <param name="size">Filled with ulong.MaxValue to indicate that the stream has an unknown length</param>
        /// <returns>true</returns>
        public override bool Open(out ulong size)
        {
            size = ulong.MaxValue;
            this._completed = false;
            return true;
        }

        /// <summary>
        /// Called by libvlc when it wants to read more data
        /// </summary>
        /// <param name="buf">The buffer pointer</param>
        /// <param name="len">The buffer length</param>
        /// <returns>The number of bytes written to the stream, 0 for EOF, -1 for error</returns>
        public unsafe override int Read(IntPtr buf, uint len)
        {
            if (this._completed)
            {
                return 0;
            }

            var readResult = this._reader.ReadAsync().AsTask().GetAwaiter().GetResult();

            if (readResult.IsCanceled)
            {
                return -1;
            }

            var buffer = (readResult.Buffer.Length > len) ? readResult.Buffer.Slice(0, len) : readResult.Buffer;
            var outputBuffer = new Span<byte>(buf.ToPointer(), (int)len);

            if (buffer.IsSingleSegment)
            {
                buffer.FirstSpan.CopyTo(outputBuffer);
            }
            else
            {
                var outputPosition = 0;
                foreach (var memory in buffer)
                {
                    memory.Span.CopyTo(outputBuffer.Slice(outputPosition));
                    outputPosition += memory.Length;
                }
            }

            var consumed = (int)buffer.Length;
            this._reader.AdvanceTo(buffer.End);

            if (readResult.IsCompleted)
            {
                this._completed = true;
            }
            return consumed;
        }

        /// <summary>
        /// Seek override that should not be called by libvlc
        /// </summary>
        /// <param name="offset">The offset at which libvlc wants to seek</param>
        /// <returns>false</returns>
        public override bool Seek(ulong offset)
        {
            return false;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<MediaLibraryData> _libraries = new ObservableCollection<MediaLibraryData>();
        public ObservableCollection<MediaLibraryData> Libraries { get { return _libraries; } set { _libraries = value; } }
        
        public List<MovieData?> Movies { get; set; } = new List<MovieData?>();
        public ObservableCollection<MovieData?> FilteredMovies { get; set; } = new ObservableCollection<MovieData?>();
        public DirectoryInfo? MovieFolder { get; set; }
        public DirectoryInfo? TVShowsFolder { get; set; }
        public List<TVSeriesData?> TVSeries { get; set; } = new();
        public ObservableCollection<TVSeriesData?> FilteredTVSeries { get; private set; } = new();

        public Frame Frame => _mainFrame;

        private TMDbClient _tmdbClient = new TMDbClient(TMDB.APIKey);
        private TMDbConfig? _tmdbConfig;

        private Timer? _searchTimer = null;

        private object MoviesLock = new object();
        private object TVShowsLock = new object();

        private MediaData _currentMedia;

        public VirtualizationCacheLength VirtualizingCacheLength { get; set; } = new VirtualizationCacheLength(100, 100);
        public VirtualizationMode VirtualizingVirtualizationMode { get; set; } = VirtualizationMode.Recycling;
        public VirtualizationCacheLengthUnit VirtualizingCacheLengthUnit { get; set; } = VirtualizationCacheLengthUnit.Pixel;
        public ScrollUnit VirtualizingScrollUnit { get; set; } = ScrollUnit.Pixel;
        public static MainWindow? Instance => App.Instance.MainWindow as MainWindow;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            LoadExistingLibraries().ContinueWith(
                (state) =>
                {
                    Dispatcher.Invoke(() =>
                          {
                              _mainFrame.Navigate(new HomePageView());
                          });
                });
        }

        private void Scroll_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
            var gridView = sender as WpfToolkit.Controls.GridView;
            if (gridView != null)
            {
                var Scroll = gridView.GetChildOfType<ScrollViewer>();
                Scroll?.ScrollToVerticalOffset(Scroll.VerticalOffset - (e.Delta / 2));
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        
        public void SaveLibraries()
        {
            foreach (var library in Libraries)
            {
                library.Save();
            }
        }

        async private Task LoadExistingLibraries()
        {
            var LibraryFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Libraries");
            if (Directory.Exists(LibraryFolderPath))
            {
                var libraryDirectory = new DirectoryInfo(LibraryFolderPath);
                var libraryFiles = await Task.Run(() => libraryDirectory.GetFiles("", SearchOption.AllDirectories));

                foreach (var library in libraryFiles)
                {
                    FileStream fs = new FileStream(library.FullName, FileMode.Open);
                    XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                    DataContractSerializer ser = new DataContractSerializer(typeof(MediaLibraryData), new List<Type>() { typeof(MovieData), typeof(TVSeriesData), typeof(TVSeasonData), typeof(TVEpisodeData) });
                    var deserializedLibrary = (MediaLibraryData?)ser.ReadObject(reader, true);
                    if (deserializedLibrary != null)
                    {
                        await deserializedLibrary.InitAfterDeserialization();
                        Libraries.Add(deserializedLibrary);
                    }
                    reader.Close();
                    fs.Close();
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer = new Timer((s) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var textBox = sender as TextBox;
                    if (textBox != null)
                    {
                        FilterMoviesAsync(textBox.Text);
                        FilterTVEpisodesAsync(textBox.Text);
                    }
                });
            }, null, TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);

        }

        private void FilterMoviesAsync(string filter = "")
        {
            if (filter.IsNullOrEmpty())
            {
                if (FilteredMovies.Count == Movies.Count)
                    return;
                FilteredMovies = new ObservableCollection<MovieData?>(Movies);
                return;
            }

            FilteredMovies = new ObservableCollection<MovieData?>(Movies);
            var filterLower = filter.ToLower();
            for (int i = FilteredMovies.Count() - 1; i >= 0; i--)
            {
                var movie = FilteredMovies[i];
                if (movie != null)
                {
                    if (!movie.Title.ToLower().Contains(filterLower))
                    {
                        FilteredMovies.RemoveAt(i);
                    }
                }
            }
        }

        private void FilterTVEpisodesAsync(string filter = "")
        {
            if (filter.IsNullOrEmpty())
            {
                if (FilteredTVSeries.Count == TVSeries.Count)
                    return;
                FilteredTVSeries = new(TVSeries);
                return;
            }

            FilteredTVSeries = new(TVSeries);
            var filterLower = filter.ToLower();
            for (int i = FilteredTVSeries.Count() - 1; i >= 0; i--)
            {
                var movie = FilteredTVSeries[i];
                if (movie != null)
                {
                    if (!movie.Title.ToLower().Contains(filterLower))
                    {
                        FilteredTVSeries.RemoveAt(i);
                    }
                }
            }
        }

        internal void ShowTVSeries(TVSeriesData? tvSeries)
        {
            _mainFrame.Navigate(new TVSeriesPageView(tvSeries));
        }

        internal void ShowTVSeason(TVSeasonData? tVSeason)
        {
            _mainFrame.Navigate(new TVSeasonPageView(tVSeason));
        }

        internal void ShowMovie(MovieData? movieData)
        {
            _mainFrame.Navigate(new MoviePageView(movieData));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mainFrame.CanGoBack)
                _mainFrame.GoBack();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new HomePageView());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_mainFrame.CanGoForward)
                _mainFrame.GoForward();
        }

        internal void ShowMediaLibrary(MediaLibraryData mediaLibraryData)
        {
            switch (mediaLibraryData.MediaLibraryType)
            {
                case MediaLibraryType.Movies:
                    _mainFrame.Navigate(new MoviesPageView(mediaLibraryData));
                    break;
                case MediaLibraryType.Music:
                    //_mainFrame.Navigate(new MusicPageView(mediaLibraryData));
                    break;
                case MediaLibraryType.TVShows:
                    _mainFrame.Navigate(new TVShowsPageView(mediaLibraryData));
                    break;
                case MediaLibraryType.Videos:
                    //_mainFrame.Navigate(new VideosPageView(mediaLibraryData));
                    break;
                default:
                    break;
            }
        }
    }
}
