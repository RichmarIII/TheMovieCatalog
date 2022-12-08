using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TheMovieCatalog.DataModels;

namespace TheMovieCatalog.ViewModels
{
    /// <summary>
    /// Interaction logic for TVShowsPageView.xaml
    /// </summary>
    public partial class TVShowsPageView : Page
    {
        public MediaLibraryData? Library { get; private set; }
        
        public TVShowsPageView()
        {
            InitializeComponent();
            DataContext = Library;
        }

        public TVShowsPageView(MediaLibraryData? library)
        {
            InitializeComponent();
            DataContext = Library = library;
        }
    }
}
