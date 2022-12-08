using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TheMovieCatalog.DataModels;
using TheMovieCatalog.Shared.ExtensionMethods;
using TheMovieCatalog.ViewModels;
using TheMovieCatalog.WebAPI;

namespace TheMovieCatalog
{
    public class DateTimeStringToYearConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (((string)value).IsNullOrEmpty())
                return null;

            return ((string)value).Substring(((string)value).LastIndexOf("/") + 1, 4);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeStringToDateStringConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value == null)
                return null;

            var StartOfTimeIndex = ((string)value).IndexOf(" ");
            if (StartOfTimeIndex == -1)
                return value;

            return ((string)value).Substring(0, StartOfTimeIndex);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeSpanToTrimmedStringConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value == null)
                return null;

            var timeSpan = (TimeSpan)value;

            return String.Format("{0:00}:{1:00}:{2:00}:{3:000}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class FilePathStringToFileNameExtConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value == null)
                return null;

            return ((string)value).Substring(((string)value).LastIndexOf("\\") + 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class SeasonNumberToStringConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value == null)
                return null;

            return "S" + value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class EpisodeNumberToStringConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value == null)
                return null;

            return "E" + value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class StorageFolderToStringConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value == null)
                return null;
            var DI = (value as DirectoryInfo);
            if (DI == null)
                return null;
            return DI.FullName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value == null)
                return null;
            var visibility = value as bool?;
            if (visibility == null)
                return null;
            return visibility == true? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    public class ChapterCountToVisibilityConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo language)
        {
            if (value == null)
                return null;
            var visibility = value as int?;
            if (visibility == null)
                return null;
            return visibility <= 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Task.Run(() =>
            {
                WebHost.CreateDefaultBuilder()
               .UseKestrel()
               .UseContentRoot(Directory.GetCurrentDirectory())
               .UseWebRoot(Directory.GetCurrentDirectory())
               .UseUrls("http://*:1234")
               .UseStartup<Startup>().Build().Run();
                Debug.WriteLine("Server Down");
            });
        }

        public static App? Instance { get { return App.Current as App; } }

        public static void PlayMedia(MediaData? mediaData)
        {
            TheaterPageView.Instance.PlayMedia(mediaData);
        }

        internal static List<MediaLibraryData> GetLibraries()
        {
            var Win = (Application.Current.MainWindow as MainWindow);
            if (Win != null)
                return Win.Libraries.ToList();
            else
                return null;
        }

        internal void ShowTVSeries(TVSeriesData? tvSeries)
        {
            var Win = (Application.Current.MainWindow as MainWindow);
            if (Win != null)
            {
                Win.ShowTVSeries(tvSeries);
            }
        }

        internal void ShowTVSeason(TVSeasonData? tvSeason)
        {
            var Win = (Application.Current.MainWindow as MainWindow);
            if (Win != null)
            {
                Win.ShowTVSeason(tvSeason);
            }
        }

        internal static void ShowMovie(MovieData? movieData)
        {
            var Win = (Application.Current.MainWindow as MainWindow);
            if (Win != null)
            {
                Win.ShowMovie(movieData);
            }
        }
    }
}
