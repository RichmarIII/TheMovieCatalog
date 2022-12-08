using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TheMovieCatalog.Shared;
using TheMovieCatalog.Shared.ExtensionMethods;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;

namespace TheMovieCatalog.DataModels
{
    [DataContract]
    public class MediaLibraryData : INotifyPropertyChanged
    {
        public static readonly string[] MediaTypes = { ".mp4", ".mkv", ".flv", ".avi", ".webm", ".ts" };
        public static readonly string[] SpecialFolderNames = { "behind the scenes", "deleted scenes", "featurettes", "interviews", "scenes", "shorts", "trailers", "other", "extras" };

        private List<MediaLibraryType> _mediaLibraryTypes = new List<MediaLibraryType>() { MediaLibraryType.Movies, MediaLibraryType.TVShows, MediaLibraryType.Music, MediaLibraryType.Videos };
        public List<MediaLibraryType> MediaLibraryTypes { get { return _mediaLibraryTypes; } set { _mediaLibraryTypes = value; NotifyPropertyChanged(); } }

        private float _scanProgress = 0;
        public float ScanProgress { get { return _scanProgress; } set { _scanProgress = value; ProgressBarVisibility = (!ScanProgress.IsApproximatelyEqual(0) && !ScanProgress.IsApproximatelyEqual(100)); NotifyPropertyChanged(); } }

        private bool _progressBarVisibility = false;
        public bool ProgressBarVisibility { get { return _progressBarVisibility; } private set { _progressBarVisibility = value; NotifyPropertyChanged(); } }

        [DataMember]
        public MediaLibraryType MediaLibraryType { get; set; }

        [DataMember]
        public List<string> MediaFiles { get; set; } = new List<string>();

        /// <summary>
        /// List Of MovieDatas For Movies Library. List Of TVSeriesDatas For TVShows Library.
        /// </summary>
        [DataMember]
        public List<object> MediaMetaDatas { get; set; } = new List<object>();

        [DataMember]
        private string _title = "";

        public string Title { get { return _title; } set { _title = value; NotifyPropertyChanged(); } }

        private DirectoryInfo? _mediaFolder;

        [DataMember]
        public string MediaFolderString { get; set; } = "";

        public DirectoryInfo MediaFolder { get => _mediaFolder; set { _mediaFolder = value; NotifyPropertyChanged(); } }

        [DataMember(Name = "_iconImage")] //Old Name. Used For Backwards Compatibility
        private string _iconImagePath;

        public string IconImagePath { get => _iconImagePath; set { _iconImagePath = value; NotifyPropertyChanged(nameof(IconImage)); } }

        private BitmapImage? _iconImage = null;
        public BitmapImage? IconImage
        {
            get
            {
                if (_iconImage == null && !IconImagePath.IsNullOrEmpty())
                {
                    LoadIconImageAsync();
                }
                return _iconImage;
            }
        }

        public bool IsScanningFiles { get; private set; }
        public bool IsGeneratingMetadata { get; private set; }

        private TMDbClient? _tmdbClient = new(TMDB.APIKey);

        private TMDbConfig? _tmdbConfig;

        public event PropertyChangedEventHandler? PropertyChanged;

        async public Task InitAfterDeserialization()
        {
            _tmdbClient = new TMDbClient(TMDB.APIKey);
            try
            {
                _tmdbConfig = await _tmdbClient.GetConfigAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            MediaFolder = new DirectoryInfo(MediaFolderString);
            if (!File.Exists(IconImagePath))
            {
                IconImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Movie_Icon.png");
            }
            Debug.Assert(MediaFolder != null);
        }

        public MediaLibraryData()
        {
            _tmdbConfig = _tmdbClient.GetConfigAsync().Result;
            IconImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Movie_Icon.png");
        }

        private void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        private async Task LoadIconImageAsync()
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
                    image.UriSource = new Uri(IconImagePath);
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
                _iconImage = image;
                NotifyPropertyChanged(nameof(IconImage));
            });
        }

        public void Save()
        {
            string LibraryFolderPath = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Libraries", Title);
            Directory.CreateDirectory(LibraryFolderPath);
            string LibraryFilePath = Path.Combine(LibraryFolderPath, "Library.xml");
            FileStream writer = new(LibraryFilePath, FileMode.Create);
            DataContractSerializer ser = new(GetType(), new List<Type>() { typeof(MovieData), typeof(TVSeriesData), typeof(TVSeasonData), typeof(TVEpisodeData) });
            ser.WriteObject(writer, this);
            writer.Close();
        }

        async public Task ScanFiles()
        {
            if (!IsScanningFiles && !IsGeneratingMetadata)
            {
                IsScanningFiles = true;
                MediaFiles.Clear();
                ScanProgress = 0;
                await Task.Run(() =>
                {
                    var mediaFiles = new DirectoryInfo(MediaFolder.FullName).GetFiles("", SearchOption.AllDirectories).OrderBy(i => i.Name);
                    var progressIncrement = 100.0f / mediaFiles.Count();
                    foreach (var mediaFile in mediaFiles)
                    {
                        if (!MediaTypes.Contains(mediaFile.Extension.ToLower()))
                        {
                            ScanProgress += progressIncrement;
                            continue;
                        }

                        MediaFiles.Add(mediaFile.FullName);
                        ScanProgress += progressIncrement;
                    }
                });
                IsScanningFiles = false;
                ScanProgress = 100;
            }
        }

        async public Task GenerateMetadata(bool bUpdateOnly = true)
        {
            if (!IsGeneratingMetadata && !IsScanningFiles)
            {
                ScanProgress = 0;
                IsGeneratingMetadata = true;
                if (MediaLibraryType == MediaLibraryType.Movies)
                    await GenerateMetadataForMovies(bUpdateOnly);
                if (MediaLibraryType == MediaLibraryType.TVShows)
                    await GenerateMetadataForTVShows(bUpdateOnly);
                IsGeneratingMetadata = false;
                ScanProgress = 100;
            }
        }

        async private Task GenerateMetadataForTVShows(bool bUpdateOnly = true)
        {
            var currentAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            List<TVEpisodeData> tvEpisodeDatas = new(MediaFiles.Count);
            float progressIncrement = 100.0f / MediaFiles.Count;
            foreach (var MediaFile in MediaFiles)
            {
                var mediaFile = new FileInfo(MediaFile);
                var MediaFileParentFolder = mediaFile.Directory;
                var FileNameMatch = Regex.Matches(mediaFile.Name, @"(?<Name>^[a-z0-9\=\-, ]+)?", RegexOptions.IgnoreCase | RegexOptions.Singleline, Regex.InfiniteMatchTimeout);
                var FolderNameMatch = Regex.Matches(MediaFileParentFolder.Name, @"(?<Name>^[a-z0-9\=\-, ]+)?", RegexOptions.IgnoreCase | RegexOptions.Singleline, Regex.InfiniteMatchTimeout);
                string FolderName = "";
                string FileName = "";

                var FolderNames = FolderNameMatch.Where((m) => m.Groups[1].Name.Equals("Name") && !m.Groups[1].Value.Equals(String.Empty));
                if (FolderNames.Count() > 0)
                    FolderName = FolderNames.First().Groups[1].Value;
                FolderName = FolderName.Trim();

                var FileNames = FileNameMatch.Where((m) => m.Groups[1].Name.Equals("Name") && !m.Groups[1].Value.Equals(String.Empty));
                if (FileNames.Count() > 0)
                    FileName = FileNames.First().Groups[1].Value;
                FileName = FileName.Trim();

                //Don't Process Extras, Only Process Files Inside Folders With The Same Name
                if (!FileName.Equals(FolderName))
                {
                    bool bShouldSkipFile = false;
                    foreach (var specialFolderName in SpecialFolderNames)
                    {
                        if (MediaFileParentFolder.FullName.ToLower().Contains(specialFolderName))
                        {
                            bShouldSkipFile = true;
                        }
                    }

                    if (bShouldSkipFile)
                    {
                        ScanProgress += progressIncrement;
                        continue;
                    }
                }

                TVEpisodeData? tvEpisodeData = null;

                var tvEpDatas = MediaMetaDatas.Select(ser =>
                {
                    var seasons = (ser as TVSeriesData).Seasons.Select((sea) =>
                    {
                        return sea.Episodes.Find((e) =>
                        {
                            return e.LocalFilePath == MediaFile;
                        });
                    }).Where((e) => e != null);
                    if (!seasons.IsNullOrEmpty())
                        return seasons.First();
                    else
                        return null;
                }).Where((e) => e != null);
                if (!tvEpDatas.IsNullOrEmpty())
                    tvEpisodeData = tvEpDatas.First();
                else
                    tvEpisodeData = new();

                if (!bUpdateOnly)
                    tvEpisodeData!.GeneratedMetaData.Clear();

                if (tvEpisodeData!.IsAllMetaDataGenerated)
                {
                    tvEpisodeDatas.Add(tvEpisodeData);
                    continue;
                }

                tvEpisodeData.LocalFilePath = mediaFile.FullName;

                TvEpisode? tvEpisode = null;

                string MetaDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetaData", mediaFile.Name);
                Directory.CreateDirectory(MetaDataFolderPath);

                string SearchTVShowName = FileName;
                var SearchYear = 0;
                var SearchSeasonNum = -1; //Important These Stay At -1
                var SearchEpisodeNum = -1;//Important These Stay At -1

                var MetaDataMatch = Regex.Matches(mediaFile.Name, @"(\[(?<Meta>[a-z0-9\=\-,]+)\])?", RegexOptions.IgnoreCase | RegexOptions.Singleline, Regex.InfiniteMatchTimeout);
                var YearMatch = Regex.Matches(mediaFile.Name, @"(\((?<Year>[0-9][0-9][0-9][0-9])\))?", RegexOptions.IgnoreCase | RegexOptions.Singleline, Regex.InfiniteMatchTimeout);
                var EpisodeDataMatch = Regex.Matches(mediaFile.Name, @"s(?<Season>[0-9]+)[ |-]*?e(?<Episode>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline, Regex.InfiniteMatchTimeout);

                var Years = YearMatch.Where((m) => m.Groups[2].Name.Equals("Year") && !m.Groups[2].Value.IsNullOrEmpty());
                if (Years.Count() > 0)
                    SearchYear = Int32.Parse(Years.First().Groups[2].Value);

                var SeasonNums = EpisodeDataMatch.Where((m) => m.Groups[1].Name.Equals("Season") && !m.Groups[1].Value.IsNullOrEmpty());
                if (SeasonNums.Count() > 0)
                    SearchSeasonNum = Int32.Parse(SeasonNums.First().Groups[1].Value);

                var EpisodeNums = EpisodeDataMatch.Where((m) => m.Groups[2].Name.Equals("Episode") && !m.Groups[2].Value.IsNullOrEmpty());
                if (EpisodeNums.Count() > 0)
                    SearchEpisodeNum = Int32.Parse(EpisodeNums.First().Groups[2].Value);

                SearchTVShowName.Trim();
                //Replace Spaces With + Because It Is A Web Query Parameter
                SearchTVShowName = SearchTVShowName.Replace(" ", "+");

                if (!tvEpisodeData.GeneratedMetaData.GetValueOrDefault(nameof(tvEpisodeData.SeriesStartYear)))
                {
                    tvEpisodeData.SeriesStartYear = SearchYear;
                    tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.SeriesStartYear), tvEpisodeData.SeriesStartYear != 0);
                }

                {
                    //Poster Image
                    {
                        if (!tvEpisodeData.GeneratedMetaData.GetValueOrDefault(nameof(tvEpisodeData.PosterImagePath)))
                        {
                            string PosterImageFilePath = Path.Combine(MetaDataFolderPath, "Poster.jpg");
                            SearchContainer<SearchTv> TVEpisodeSearch = null;
                            if (tvEpisode == null)
                            {
                                TVEpisodeSearch = await _tmdbClient.SearchTvShowAsync(SearchTVShowName, 0, true, SearchYear);
                                if (!TVEpisodeSearch.Results.IsNullOrEmpty())
                                    tvEpisode = await _tmdbClient.GetTvEpisodeAsync(TVEpisodeSearch.Results.First().Id, SearchSeasonNum, SearchEpisodeNum);
                            }
                            if (tvEpisode != null)
                            {
                                if (!tvEpisode.StillPath.IsNullOrEmpty())
                                {
                                    var uri = _tmdbClient.GetImageUrl("w300", tvEpisode.StillPath);
                                    try

                                    {
                                        await HelperMethods.DownloadFileAsync(uri, PosterImageFilePath);

                                        tvEpisodeData.PosterImagePath = PosterImageFilePath;
                                        tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.PosterImagePath), !tvEpisodeData.PosterImagePath.IsNullOrEmpty());
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex);
                                    }
                                }
                                else
                                {
                                    if (TVEpisodeSearch != null)
                                    {
                                        string showPosterPath = TVEpisodeSearch.Results.First().PosterPath;
                                        if (!showPosterPath.IsNullOrEmpty())
                                        {
                                            var uri = _tmdbClient.GetImageUrl("w300", showPosterPath);
                                            try
                                            {
                                                await HelperMethods.DownloadFileAsync(uri, PosterImageFilePath);

                                                tvEpisodeData.PosterImagePath = PosterImageFilePath;
                                                tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.PosterImagePath), !tvEpisodeData.PosterImagePath.IsNullOrEmpty());
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine(ex);
                                            }
                                        }
                                    }
                                }
                            }

                            if (tvEpisodeData.PosterImagePath.IsNullOrEmpty())
                            {
                                tvEpisodeData.PosterImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "TVEpisodePlaceholder.png");
                            }
                        }
                    }

                    //Backdrop Image
                    {
                        if (!tvEpisodeData.GeneratedMetaData.GetValueOrDefault(nameof(tvEpisodeData.BackdropImagePath)))
                        {
                            string BackdropImageFilePath = Path.Combine(MetaDataFolderPath, "Backdrop.jpg");
                            SearchContainer<SearchTv> TVEpisodeSearch = null;
                            if (tvEpisode == null)
                            {
                                TVEpisodeSearch = await _tmdbClient.SearchTvShowAsync(SearchTVShowName, 0, true, SearchYear);
                                if (!TVEpisodeSearch.Results.IsNullOrEmpty())
                                    tvEpisode = await _tmdbClient.GetTvEpisodeAsync(TVEpisodeSearch.Results.First().Id, SearchSeasonNum, SearchEpisodeNum);
                            }
                            if (tvEpisode != null)
                            {
                                if (!tvEpisode.StillPath.IsNullOrEmpty())
                                {
                                    var uri = _tmdbClient.GetImageUrl("original", tvEpisode.StillPath);
                                    try

                                    {
                                        await HelperMethods.DownloadFileAsync(uri, BackdropImageFilePath);
                                        tvEpisodeData.BackdropImagePath = BackdropImageFilePath;
                                        tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.BackdropImagePath), !tvEpisodeData.BackdropImagePath.IsNullOrEmpty());
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex);
                                    }
                                }
                                else
                                {
                                    if (TVEpisodeSearch != null)
                                    {
                                        string showPosterPath = TVEpisodeSearch.Results.First().BackdropPath;
                                        if (!showPosterPath.IsNullOrEmpty())
                                        {
                                            var uri = _tmdbClient.GetImageUrl("original", showPosterPath);
                                            try
                                            {
                                                await HelperMethods.DownloadFileAsync(uri, BackdropImageFilePath);
                                                tvEpisodeData.BackdropImagePath = BackdropImageFilePath;
                                                tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.BackdropImagePath), !tvEpisodeData.BackdropImagePath.IsNullOrEmpty());
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine(ex);
                                            }
                                        }
                                    }
                                }
                            }

                            if (tvEpisodeData.BackdropImagePath.IsNullOrEmpty())
                            {
                                tvEpisodeData.BackdropImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "TVEpisodePlaceholder.png");
                            }
                        }
                    }

                    //Title
                    {
                        if (!tvEpisodeData.GeneratedMetaData.GetValueOrDefault(nameof(tvEpisodeData.Title)))
                        {
                            var TVEpisodeSearch = await _tmdbClient.SearchTvShowAsync(SearchTVShowName, 0, true, SearchYear);
                            if (tvEpisode == null)
                            {
                                if (!TVEpisodeSearch.Results.IsNullOrEmpty())
                                    tvEpisode = await _tmdbClient.GetTvEpisodeAsync(TVEpisodeSearch.Results.First().Id, SearchSeasonNum, SearchEpisodeNum);
                            }
                            if (tvEpisode != null)
                            {
                                tvEpisodeData.Title = tvEpisode.Name;
                                tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.Title), !tvEpisodeData.Title.IsNullOrEmpty());
                            }
                            if (tvEpisode == null)
                            {
                                tvEpisodeData.Title = mediaFile.Name;
                            }
                        }
                    }

                    //Series Name
                    {
                        if (!tvEpisodeData.GeneratedMetaData.GetValueOrDefault(nameof(tvEpisodeData.SeriesName)))
                        {
                            var TVEpisodeSearch = await _tmdbClient.SearchTvShowAsync(SearchTVShowName, 0, true, SearchYear);
                            if (tvEpisode == null)
                            {
                                if (!TVEpisodeSearch.Results.IsNullOrEmpty())
                                    tvEpisode = await _tmdbClient.GetTvEpisodeAsync(TVEpisodeSearch.Results.First().Id, SearchSeasonNum, SearchEpisodeNum);
                            }
                            if (tvEpisode != null)
                            {
                                tvEpisodeData.SeriesName = TVEpisodeSearch.Results.First().Name;
                                tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.SeriesName), !tvEpisodeData.SeriesName.IsNullOrEmpty());
                                if (tvEpisodeData.SeriesName.IsNullOrEmpty())
                                {
                                    tvEpisodeData.SeriesName = SearchTVShowName.Replace("+", " ");
                                }
                            }
                            if (tvEpisodeData.SeriesName.IsNullOrEmpty())
                            {
                                tvEpisodeData.SeriesName = SearchTVShowName.Replace("+", " ");
                            }
                        }
                    }

                    //Description
                    {
                        if (!tvEpisodeData.GeneratedMetaData.GetValueOrDefault(nameof(tvEpisodeData.Description)))
                        {
                            if (tvEpisode == null)
                            {
                                var TVEpisodeSearch = await _tmdbClient.SearchTvShowAsync(SearchTVShowName, 0, true, SearchYear);
                                if (!TVEpisodeSearch.Results.IsNullOrEmpty())
                                    tvEpisode = await _tmdbClient.GetTvEpisodeAsync(TVEpisodeSearch.Results.First().Id, SearchSeasonNum, SearchEpisodeNum);
                            }
                            if (tvEpisode != null)
                            {
                                tvEpisodeData.Description = tvEpisode.Overview;
                                tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.Description), !tvEpisodeData.Description.IsNullOrEmpty());
                            }
                        }
                    }

                    //Release Date
                    {
                        if (!tvEpisodeData.GeneratedMetaData.GetValueOrDefault(nameof(tvEpisodeData.ReleaseDate)))
                        {
                            if (tvEpisode == null)
                            {
                                var TVEpisodeSearch = await _tmdbClient.SearchTvShowAsync(SearchTVShowName, 0, true, SearchYear);
                                if (!TVEpisodeSearch.Results.IsNullOrEmpty())
                                    tvEpisode = await _tmdbClient.GetTvEpisodeAsync(TVEpisodeSearch.Results.First().Id, SearchSeasonNum, SearchEpisodeNum);
                            }
                            if (tvEpisode != null)
                            {
                                tvEpisodeData.ReleaseDate = tvEpisode.AirDate.HasValue ? tvEpisode.AirDate.Value.ToString() : "";
                                tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.ReleaseDate), !tvEpisodeData.ReleaseDate.IsNullOrEmpty());
                            }
                        }
                    }

                    //Season Number
                    {
                        if (!tvEpisodeData.GeneratedMetaData.GetValueOrDefault(nameof(tvEpisodeData.SeasonNumber)))
                        {
                            if (tvEpisode == null)
                            {
                                var TVEpisodeSearch = await _tmdbClient.SearchTvShowAsync(SearchTVShowName, 0, true, SearchYear);
                                if (!TVEpisodeSearch.Results.IsNullOrEmpty())
                                    tvEpisode = await _tmdbClient.GetTvEpisodeAsync(TVEpisodeSearch.Results.First().Id, SearchSeasonNum, SearchEpisodeNum);
                            }
                            if (tvEpisode != null)
                            {
                                tvEpisodeData.SeasonNumber = tvEpisode.SeasonNumber;
                                tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.ReleaseDate), tvEpisodeData.SeasonNumber >= 0);
                            }
                        }
                    }

                    //Episode Number
                    {
                        if (!tvEpisodeData.GeneratedMetaData.GetValueOrDefault(nameof(tvEpisodeData.EpisodeNumber)))
                        {
                            if (tvEpisode == null)
                            {
                                var TVEpisodeSearch = await _tmdbClient.SearchTvShowAsync(SearchTVShowName, 0, true, SearchYear);
                                if (!TVEpisodeSearch.Results.IsNullOrEmpty())
                                    tvEpisode = await _tmdbClient.GetTvEpisodeAsync(TVEpisodeSearch.Results.First().Id, SearchSeasonNum, SearchEpisodeNum);
                            }
                            if (tvEpisode != null)
                            {
                                tvEpisodeData.EpisodeNumber = tvEpisode.EpisodeNumber;
                                tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.EpisodeNumber), tvEpisodeData.EpisodeNumber >= 0);
                            }
                        }
                    }
                }

                tvEpisodeData.GeneratedMetaData.AddOrUpdate(nameof(tvEpisodeData.LocalFilePath), !tvEpisodeData.LocalFilePath.IsNullOrEmpty());
                Debug.Assert(!tvEpisodeData.SeriesName.IsNullOrEmpty());
                tvEpisodeData.HasEverGeneratedMetaData = true;
                tvEpisodeDatas.Add(tvEpisodeData);
                ScanProgress += progressIncrement;
            }

            //Generate Series And Season Datas
            ScanProgress = 0;
            progressIncrement = 100.0f / tvEpisodeDatas.Count();
            List<object> mediaMetaDatas = new List<object>();
            foreach (var tvEpisode in tvEpisodeDatas)
            {
                string SeriesMetaDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetaData", tvEpisode.SeriesName.Replace(":", " "));
                Directory.CreateDirectory(SeriesMetaDataFolderPath);

                string SeasonMetaDataFolderPath = Path.Combine(SeriesMetaDataFolderPath, "Season " + tvEpisode.SeasonNumber);
                Directory.CreateDirectory(SeasonMetaDataFolderPath);

                var series = (TVSeriesData?)mediaMetaDatas.Find(x => ((TVSeriesData)x).Title.ToLower() == tvEpisode.SeriesName.ToLower());
                if (series == null)
                {
                    //Generate Series MetaData
                    series = new();

                    {
                        SearchContainer<SearchTv> TVShowSearch = await _tmdbClient.SearchTvShowAsync(tvEpisode.SeriesName, 0, true, tvEpisode.SeriesStartYear);
                        TvShow? foundSeries = null;
                        if (TVShowSearch != null && !TVShowSearch.Results.IsNullOrEmpty())
                            foundSeries = await _tmdbClient.GetTvShowAsync(TVShowSearch.Results.First().Id);

                        //Poster Image
                        {
                            if (!series.GeneratedMetaData.GetValueOrDefault(nameof(series.PosterImagePath)))
                            {
                                string PosterImageFilePath = Path.Combine(SeriesMetaDataFolderPath, "Poster.jpg");
                                if (foundSeries != null)
                                {
                                    if (!foundSeries.PosterPath.IsNullOrEmpty())
                                    {
                                        var uri = _tmdbClient.GetImageUrl("w300", foundSeries.PosterPath);
                                        try
                                        {
                                            await HelperMethods.DownloadFileAsync(uri, PosterImageFilePath);

                                            series.PosterImagePath = PosterImageFilePath;
                                            series.GeneratedMetaData.AddOrUpdate(nameof(series.PosterImagePath), !series.PosterImagePath.IsNullOrEmpty());
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine(ex);
                                        }
                                    }
                                }

                                if (series.PosterImagePath.IsNullOrEmpty())
                                {
                                    series.PosterImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "TVShowPlaceholder.png");
                                }
                            }
                        }

                        //Backdrop Image
                        {
                            if (!series.GeneratedMetaData.GetValueOrDefault(nameof(series.BackdropImagePath)))
                            {
                                string BackdropImageFilePath = Path.Combine(SeriesMetaDataFolderPath, "Backdrop.jpg");
                                if (foundSeries != null)
                                {
                                    if (!foundSeries.PosterPath.IsNullOrEmpty())
                                    {
                                        var uri = _tmdbClient.GetImageUrl("original", foundSeries.BackdropPath);
                                        try
                                        {
                                            await HelperMethods.DownloadFileAsync(uri, BackdropImageFilePath);

                                            series.BackdropImagePath = BackdropImageFilePath;
                                            series.GeneratedMetaData.AddOrUpdate(nameof(series.BackdropImagePath), !series.BackdropImagePath.IsNullOrEmpty());
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine(ex);
                                        }
                                    }
                                }

                                if (series.BackdropImagePath.IsNullOrEmpty())
                                {
                                    series.BackdropImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "TVShowPlaceholder.png");
                                }
                            }
                        }

                        //Title
                        {
                            if (!series.GeneratedMetaData.GetValueOrDefault(nameof(series.Title)))
                            {
                                if (foundSeries != null)
                                {
                                    series.Title = foundSeries.Name;
                                    series.GeneratedMetaData.AddOrUpdate(nameof(series.Title), !series.Title.IsNullOrEmpty());
                                }
                                if (series.Title.IsNullOrEmpty())
                                {
                                    series.Title = tvEpisode.SeriesName;
                                }
                            }
                        }

                        //Description
                        {
                            if (!series.GeneratedMetaData.GetValueOrDefault(nameof(series.Description)))
                            {
                                if (foundSeries != null)
                                {
                                    series.Description = foundSeries.Overview;
                                    series.GeneratedMetaData.AddOrUpdate(nameof(series.Description), !series.Description.IsNullOrEmpty());
                                }
                            }
                        }

                        //Release Date Start
                        {
                            if (!series.GeneratedMetaData.GetValueOrDefault(nameof(series.ReleaseDateStart)))
                            {
                                if (foundSeries != null)
                                {
                                    series.ReleaseDateStart = foundSeries.FirstAirDate.HasValue ? foundSeries.FirstAirDate.Value.ToString() : "";
                                    series.GeneratedMetaData.AddOrUpdate(nameof(series.ReleaseDateStart), !series.ReleaseDateStart.IsNullOrEmpty());
                                }
                                if (series.ReleaseDateStart.IsNullOrEmpty())
                                    series.ReleaseDateStart = "1900";
                            }
                        }

                        //Release Date End
                        {
                            if (!series.GeneratedMetaData.GetValueOrDefault(nameof(series.ReleaseDateEnd)))
                            {
                                if (foundSeries != null)
                                {
                                    series.ReleaseDateEnd = foundSeries.LastAirDate.HasValue ? foundSeries.LastAirDate.Value.ToString() : "";
                                    series.GeneratedMetaData.AddOrUpdate(nameof(series.ReleaseDateEnd), !series.ReleaseDateEnd.IsNullOrEmpty());
                                }
                                if (series.ReleaseDateEnd.IsNullOrEmpty())
                                    series.ReleaseDateEnd = "1900";
                            }
                        }
                    }

                    series.HasEverGeneratedMetaData = true;

                    mediaMetaDatas.Add(series);
                }

                var season = series?.Seasons.Find(x => x.SeasonNumber == tvEpisode.SeasonNumber);
                if (season == null)
                {
                    //Generate Season MetaData
                    season = new();

                    {
                        SearchContainer<SearchTv> TVShowSearch = await _tmdbClient.SearchTvShowAsync(tvEpisode.SeriesName, 0, true, tvEpisode.SeriesStartYear);
                        TvSeason? foundSeason = null;
                        if (TVShowSearch != null && !TVShowSearch.Results.IsNullOrEmpty())
                            foundSeason = await _tmdbClient.GetTvSeasonAsync(TVShowSearch.Results.First().Id, tvEpisode.SeasonNumber);

                        //Poster Image
                        {
                            if (!season.GeneratedMetaData.GetValueOrDefault(nameof(season.PosterImagePath)))
                            {
                                string PosterImageFilePath = Path.Combine(SeasonMetaDataFolderPath, "Poster.jpg");
                                if (foundSeason != null)
                                {
                                    if (!foundSeason.PosterPath.IsNullOrEmpty())
                                    {
                                        var uri = _tmdbClient.GetImageUrl("w300", foundSeason.PosterPath);
                                        try

                                        {
                                            await HelperMethods.DownloadFileAsync(uri, PosterImageFilePath);

                                            season.PosterImagePath = PosterImageFilePath;
                                            season.GeneratedMetaData.AddOrUpdate(nameof(season.PosterImagePath), !season.PosterImagePath.IsNullOrEmpty());
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine(ex);
                                        }
                                    }
                                }

                                if (season.PosterImagePath.IsNullOrEmpty())
                                {
                                    season.PosterImagePath = series.PosterImagePath;
                                    season.GeneratedMetaData.AddOrUpdate(nameof(season.PosterImagePath), !season.PosterImagePath.IsNullOrEmpty());
                                }

                                if (season.PosterImagePath.IsNullOrEmpty())
                                {
                                    season.PosterImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "TVSeasonPlaceholder.png");
                                }
                            }
                        }

                        //Backdrop Image
                        {
                            if (!season.GeneratedMetaData.GetValueOrDefault(nameof(season.BackdropImagePath)))
                            {
                                season.BackdropImagePath = series.BackdropImagePath;
                                season.GeneratedMetaData.AddOrUpdate(nameof(season.BackdropImagePath), !season.BackdropImagePath.IsNullOrEmpty());

                                if (season.BackdropImagePath.IsNullOrEmpty())
                                {
                                    season.BackdropImagePath = tvEpisode.BackdropImagePath;
                                    season.GeneratedMetaData.AddOrUpdate(nameof(season.BackdropImagePath), !season.BackdropImagePath.IsNullOrEmpty());
                                }

                                if (season.BackdropImagePath.IsNullOrEmpty())
                                {
                                    season.BackdropImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "TVEpisodePlaceholder.png");
                                }
                            }
                        }

                        //Title
                        {
                            if (!season.GeneratedMetaData.GetValueOrDefault(nameof(season.Title)))
                            {
                                if (foundSeason != null)
                                {
                                    season.Title = foundSeason.Name;
                                    season.GeneratedMetaData.AddOrUpdate(nameof(season.Title), !season.Title.IsNullOrEmpty());
                                }
                                if (season.Title.IsNullOrEmpty())
                                {
                                    season.Title = "Unknown Season";
                                }
                            }
                        }

                        //Description
                        {
                            if (!season.GeneratedMetaData.GetValueOrDefault(nameof(season.Description)))
                            {
                                if (foundSeason != null)
                                {
                                    season.Description = foundSeason.Overview;
                                    season.GeneratedMetaData.AddOrUpdate(nameof(season.Description), !season.Description.IsNullOrEmpty());
                                }
                            }
                        }

                        //Release Date
                        {
                            if (!season.GeneratedMetaData.GetValueOrDefault(nameof(season.ReleaseDate)))
                            {
                                if (foundSeason != null)
                                {
                                    season.ReleaseDate = foundSeason.AirDate.HasValue ? foundSeason.AirDate.Value.ToString() : "";
                                    season.GeneratedMetaData.AddOrUpdate(nameof(season.ReleaseDate), !season.ReleaseDate.IsNullOrEmpty());
                                }
                                if (season.ReleaseDate.IsNullOrEmpty())
                                {
                                    season.ReleaseDate = "1900";
                                }
                            }
                        }

                        //Season Number
                        {
                            if (!season.GeneratedMetaData.GetValueOrDefault(nameof(season.SeasonNumber)))
                            {
                                if (foundSeason != null)
                                {
                                    season.SeasonNumber = foundSeason.SeasonNumber;
                                    season.GeneratedMetaData.AddOrUpdate(nameof(season.SeasonNumber), season.SeasonNumber != -1);
                                }
                            }
                        }
                    }

                    season.SeriesID = series.ID;

                    season.HasEverGeneratedMetaData = true;

                    series.Seasons.Add(season);
                }
                tvEpisode.SeriesID = series.ID;
                tvEpisode.SeasonID = season.ID;
                season.Episodes.Add(tvEpisode);
                ScanProgress += progressIncrement;
            }
            mediaMetaDatas.ForEach(x => ((TVSeriesData)x).Seasons = ((TVSeriesData)x).Seasons.OrderBy(sea => sea.SeasonNumber).ToList());
            MediaMetaDatas = mediaMetaDatas.OrderBy(x => ((TVSeriesData)x).Title).ToList();
        }

        async private Task GenerateMetadataForMovies(bool bUpdateOnly = true)
        {
            var progressIncrement = 100.0f / MediaFiles.Count;
            List<object> movieDatas = new(MediaFiles.Count);
            foreach (var MediaFile in MediaFiles)
            {
                MovieData? mediaData = null;

                FileInfo mediaFile = new(MediaFile);
                var MediaFileParentFolder = mediaFile.Directory;
                var FileNameMatch = Regex.Matches(mediaFile.Name, @"(?<Name>^[a-z0-9\=\-, ]+)?", RegexOptions.IgnoreCase | RegexOptions.Singleline, Regex.InfiniteMatchTimeout);
                var FolderNameMatch = Regex.Matches(MediaFileParentFolder!.Name, @"(?<Name>^[a-z0-9\=\-, ]+)?", RegexOptions.IgnoreCase | RegexOptions.Singleline, Regex.InfiniteMatchTimeout);
                string FolderName = "";
                string FileName = "";

                var FolderNames = FolderNameMatch.Where((m) => m.Groups[1].Name.Equals("Name") && !m.Groups[1].Value.IsNullOrEmpty());
                if (FolderNames.Count() > 0)
                    FolderName = FolderNames.First().Groups[1].Value;
                FolderName = FolderName.Trim();

                var FileNames = FileNameMatch.Where((m) => m.Groups[1].Name.Equals("Name") && !m.Groups[1].Value.IsNullOrEmpty());
                if (FileNames.Count() > 0)
                    FileName = FileNames.First().Groups[1].Value;
                FileName = FileName.Trim();

                //Don't Process Extras, Only Process Files Inside Folders With The Same Name
                if (!FileName.Equals(FolderName))
                {
                    bool bShoudSkipFile = false;
                    foreach (var specialFolderName in SpecialFolderNames)
                    {
                        if (MediaFileParentFolder.FullName.ToLower().Contains(specialFolderName))
                        {
                            bShoudSkipFile = true;
                            break;
                        }
                    }

                    if (bShoudSkipFile)
                    {
                        ScanProgress += progressIncrement;
                        continue;
                    }
                }

                var movieData = MediaMetaDatas.Find(m =>
                {
                    return ((MovieData)m).LocalFilePath == MediaFile;
                });
                if (movieData != null)
                    mediaData = movieData as MovieData;
                else
                    mediaData = new();

                if (!bUpdateOnly)
                    mediaData!.GeneratedMetaData.Clear();

                if (mediaData!.IsAllMetaDataGenerated)
                {
                    movieDatas.Add(mediaData);
                    continue;
                }

                SearchMovie? SearchMovie = null;

                string MetaDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetaData", "Library", Title, mediaFile.Name);
                Directory.CreateDirectory(MetaDataFolderPath);

                string SearchMovieName = FileName;
                var SearchYear = 0;

                var MetaDataMatch = Regex.Matches(mediaFile.Name, @"(\[(?<Meta>[a-z0-9\=\-,]+)\])?", RegexOptions.IgnoreCase | RegexOptions.Singleline, Regex.InfiniteMatchTimeout);
                var YearMatch = Regex.Matches(mediaFile.Name, @"(\((?<Year>[0-9][0-9][0-9][0-9])\))?", RegexOptions.IgnoreCase | RegexOptions.Singleline, Regex.InfiniteMatchTimeout);

                var Years = YearMatch.Where((m) => m.Groups[2].Name.Equals("Year") && !m.Groups[2].Value.IsNullOrEmpty());
                if (!Years.IsNullOrEmpty())
                    SearchYear = Int32.Parse(Years.First().Groups[2].Value);

                string movieid = "";

                var Metas = MetaDataMatch.Where((m) => m.Groups[2].Name.Equals("Meta") && !m.Groups[2].Value.IsNullOrEmpty());
                if (!Metas.IsNullOrEmpty())
                {
                    Metas = Metas.Where((m) => (m.Groups[2].Value.Contains("imdb") || m.Groups[2].Value.Contains("tmdb")) && !m.Groups[2].Value.IsNullOrEmpty());
                    if (!Metas.IsNullOrEmpty())
                    {
                        string movieIDMeta = Metas.First().Groups[2].Value;
                        movieid = movieIDMeta.Substring(movieIDMeta.IndexOf("=") + 1);
                    }
                }

                SearchMovieName.Trim();
                //Replace Spaces With + Because It Is A Web Query Parameter
                SearchMovieName = SearchMovieName.Replace(" ", "+");

                if (!movieid.IsNullOrEmpty())
                {
                    var movie = await _tmdbClient.GetMovieAsync(movieid);
                    if (movie != null)
                    {
                        {
                            //Poster Image
                            {
                                if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.PosterImagePath)))
                                {
                                    string PosterImageFilePath = Path.Combine(MetaDataFolderPath, "Poster.jpg");
                                    var uri = _tmdbClient.GetImageUrl("w300", movie.PosterPath);
                                    try
                                    {
                                        await HelperMethods.DownloadFileAsync(uri, PosterImageFilePath);

                                        mediaData.PosterImagePath = PosterImageFilePath;
                                        mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.PosterImagePath), !mediaData.PosterImagePath.IsNullOrEmpty());
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex.Message);
                                    } 
                                }
                            }

                            //Backdrop Image
                            {
                                if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.BackdropImagePath)))
                                {
                                    string BackdropImageFilePath = Path.Combine(MetaDataFolderPath, "Backdrop.jpg");
                                    var uri = _tmdbClient.GetImageUrl("original", movie.BackdropPath);
                                    try
                                    {
                                        await HelperMethods.DownloadFileAsync(uri, BackdropImageFilePath);

                                        mediaData.BackdropImagePath = BackdropImageFilePath;
                                        mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.BackdropImagePath), !mediaData.BackdropImagePath.IsNullOrEmpty());
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex.Message);
                                    }
                                }
                            }

                            //Title
                            {
                                if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.Title)))
                                {
                                    mediaData.Title = movie.Title;
                                    mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.Title), !mediaData.Title.IsNullOrEmpty());
                                    if (mediaData.Title.IsNullOrEmpty())
                                        mediaData.Title = SearchMovieName.Replace("+", " "); 
                                }
                            }

                            //Description
                            {
                                if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.Description)))
                                {
                                    mediaData.Description = movie.Overview;
                                    mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.Description), !mediaData.Description.IsNullOrEmpty()); 
                                }
                            }

                            //Release Date
                            {
                                if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.ReleaseDate)))
                                {
                                    mediaData.ReleaseDate = movie.ReleaseDate.HasValue ? movie.ReleaseDate.Value.ToString() : "";
                                    mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.ReleaseDate), !mediaData.ReleaseDate.IsNullOrEmpty());
                                    if (mediaData.ReleaseDate.IsNullOrEmpty())
                                        mediaData.ReleaseDate = "1900"; 
                                }
                            }

                            //MPAA Rating
                            {
                                if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.MPAARating)))
                                {
                                    var ReleaseDates = await _tmdbClient.GetMovieReleaseDatesAsync(movie.Id);
                                    if (ReleaseDates != null)
                                    {
                                        var USReleases = ReleaseDates.Results.Where((i) => { return i.Iso_3166_1.ToLower().Equals("us"); });
                                        if (!USReleases.IsNullOrEmpty())
                                        {
                                            var TheatricalRelease = USReleases.First().ReleaseDates.Where((i) => { return !i.Certification.IsNullOrEmpty(); });
                                            if (!TheatricalRelease.IsNullOrEmpty())
                                            {
                                                mediaData.MPAARating = TheatricalRelease.First().Certification;
                                                mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.MPAARating), !mediaData.MPAARating.IsNullOrEmpty());
                                            }
                                            if (mediaData.MPAARating.IsNullOrEmpty())
                                            {
                                                mediaData.MPAARating = "NR";
                                            }
                                        }
                                    } 
                                }
                            }
                        }
                    }
                }

                else
                {
                    {
                        //Poster Image
                        {
                            if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.PosterImagePath)))
                            {
                                string PosterImageFilePath = Path.Combine(MetaDataFolderPath, "Poster.jpg");
                                if (SearchMovie == null)
                                {
                                    var MovieSearch = await _tmdbClient.SearchMovieAsync(SearchMovieName, 0, true, SearchYear);
                                    SearchMovie = MovieSearch.Results.Count > 0 ? MovieSearch.Results.First() : null;
                                }
                                if (SearchMovie != null)
                                {
                                    var uri = _tmdbClient.GetImageUrl("w300", SearchMovie.PosterPath);
                                    try
                                    {
                                        await HelperMethods.DownloadFileAsync(uri, PosterImageFilePath);

                                        mediaData.PosterImagePath = PosterImageFilePath;
                                        mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.PosterImagePath), !mediaData.PosterImagePath.IsNullOrEmpty());
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex.Message);
                                    }
                                } 
                            }
                        }

                        //Backdrop Image
                        {
                            if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.BackdropImagePath)))
                            {
                                string BackdropImageFilePath = Path.Combine(MetaDataFolderPath, "Backdrop.jpg");
                                if (SearchMovie == null)
                                {
                                    var MovieSearch = await _tmdbClient.SearchMovieAsync(SearchMovieName, 0, true, SearchYear);
                                    SearchMovie = MovieSearch.Results.Count > 0 ? MovieSearch.Results.First() : null;
                                }
                                if (SearchMovie != null)
                                {
                                    var uri = _tmdbClient.GetImageUrl("original", SearchMovie.BackdropPath);
                                    try
                                    {
                                        await HelperMethods.DownloadFileAsync(uri, BackdropImageFilePath);

                                        mediaData.BackdropImagePath = BackdropImageFilePath;
                                        mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.BackdropImagePath), !mediaData.BackdropImagePath.IsNullOrEmpty());
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex.Message);
                                    }
                                } 
                            }
                        }

                        //Title
                        {
                            if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.Title)))
                            {
                                if (SearchMovie == null)
                                {
                                    var MovieSearch = await _tmdbClient.SearchMovieAsync(SearchMovieName, 0, true, SearchYear);
                                    SearchMovie = MovieSearch.Results.Count > 0 ? MovieSearch.Results.First() : null;
                                }
                                if (SearchMovie != null)
                                {
                                    mediaData.Title = SearchMovie.Title;
                                    mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.Title), !mediaData.Title.IsNullOrEmpty());
                                }
                                if (mediaData.Title.IsNullOrEmpty())
                                {
                                    mediaData.Title = SearchMovieName.Replace("+", " ");
                                } 
                            }
                        }

                        //Description
                        {
                            if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.Description)))
                            {
                                if (SearchMovie == null)
                                {
                                    var MovieSearch = await _tmdbClient.SearchMovieAsync(SearchMovieName, 0, true, SearchYear);
                                    SearchMovie = MovieSearch.Results.Count > 0 ? MovieSearch.Results.First() : null;
                                }
                                if (SearchMovie != null)
                                {
                                    mediaData.Description = SearchMovie.Overview;
                                    mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.Description), !mediaData.Description.IsNullOrEmpty());
                                } 
                            }
                        }

                        //Release Date
                        {
                            if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.ReleaseDate)))
                            {
                                if (SearchMovie == null)
                                {
                                    var MovieSearch = await _tmdbClient.SearchMovieAsync(SearchMovieName, 0, true, SearchYear);
                                    SearchMovie = MovieSearch.Results.Count > 0 ? MovieSearch.Results.First() : null;
                                }
                                if (SearchMovie != null)
                                {
                                    mediaData.ReleaseDate = SearchMovie.ReleaseDate.HasValue ? SearchMovie.ReleaseDate.Value.ToString() : "";
                                    mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.ReleaseDate), !mediaData.ReleaseDate.IsNullOrEmpty());
                                }
                                if (mediaData.ReleaseDate.IsNullOrEmpty())
                                    mediaData.ReleaseDate = "1900"; 
                            }
                        }

                        //MPAA Rating
                        {
                            if (!mediaData.GeneratedMetaData.GetValueOrDefault(nameof(mediaData.MPAARating)))
                            {
                                if (SearchMovie == null)
                                {
                                    var MovieSearch = await _tmdbClient?.SearchMovieAsync(SearchMovieName, 0, true, SearchYear);
                                    SearchMovie = MovieSearch.Results.Count > 0 ? MovieSearch.Results.First() : null;
                                }
                                if (SearchMovie != null)
                                {
                                    var ReleaseDates = await _tmdbClient.GetMovieReleaseDatesAsync(SearchMovie.Id);
                                    if (ReleaseDates != null)
                                    {
                                        var USReleases = ReleaseDates.Results.Where((i) => { return i.Iso_3166_1.ToLower().Equals("us"); });
                                        if (!USReleases.IsNullOrEmpty())
                                        {
                                            var TheatricalRelease = USReleases.First().ReleaseDates.Where((i) => { return !i.Certification.IsNullOrEmpty(); });
                                            if (!TheatricalRelease.IsNullOrEmpty())
                                            {
                                                mediaData.MPAARating = TheatricalRelease.First().Certification;
                                                mediaData.GeneratedMetaData.AddOrUpdate(nameof(mediaData.MPAARating), !mediaData.MPAARating.IsNullOrEmpty());
                                            }
                                            if (mediaData.MPAARating.IsNullOrEmpty())
                                                mediaData.MPAARating = "NR";
                                        }
                                    }
                                } 
                            }
                        }
                    }
                }

                mediaData.LocalFilePath = mediaFile.FullName;
                mediaData.HasEverGeneratedMetaData = true;
                movieDatas.Add(mediaData);
                ScanProgress += progressIncrement;
            }
            MediaMetaDatas = movieDatas;
        }
    }
}