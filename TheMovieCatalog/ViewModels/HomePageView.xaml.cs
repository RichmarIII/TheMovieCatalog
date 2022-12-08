using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TheMovieCatalog.DataModels;
using TheMovieCatalog.Shared.ExtensionMethods;

namespace TheMovieCatalog.ViewModels
{
    /// <summary>
    /// Interaction logic for HomePageView.xaml
    /// </summary>
    public partial class HomePageView : Page
    {

        public static ObservableCollection<MediaLibraryData> Libraries { get { return MainWindow.Instance.Libraries; } }

        public HomePageView()
        {
            InitializeComponent();
            this.DataContext = this;
        }


        async private void AddLibrary_Tapped(object sender, RoutedEventArgs e)
        {
            var mediaLibraryData = new MediaLibraryData();
            var LibraryEditViewControl = new ViewModels.LibraryEditView(mediaLibraryData);
            var NewLibraryDialog = new Window();
            NewLibraryDialog.Content = LibraryEditViewControl;
            NewLibraryDialog.Title = "New Library";
            var Result = NewLibraryDialog.ShowDialog();
            if (Result != null && Result.Value)
            {
                if (mediaLibraryData != null && !mediaLibraryData.Title.IsNullOrEmpty() && mediaLibraryData.IconImage != null)
                {
                    Libraries.Add(mediaLibraryData);
                    MessageBox.Show("Library Added!");
                    await mediaLibraryData.ScanFiles();
                    await mediaLibraryData.GenerateMetadata(false);
                    _libraryFlipView.SelectedItem = mediaLibraryData;
                    MainWindow.Instance?.ShowMediaLibrary(mediaLibraryData);
                    MainWindow.Instance?.SaveLibraries();
                }
                else
                    MessageBox.Show("Library Not Added! Not All Data Was Provided");
            }
            else
            {
                MessageBox.Show("Library Not Added! User Closed");
            }
        }
    }
}
