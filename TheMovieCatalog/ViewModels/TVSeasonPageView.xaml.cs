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
using TMDbLib.Objects.TvShows;

namespace TheMovieCatalog.ViewModels
{
    /// <summary>
    /// Interaction logic for TVSeriesPageView.xaml
    /// </summary>
    public partial class TVSeasonPageView : Page
    {
        public TVSeasonData? TVSeason { get; private set; }
        
        public TVSeasonPageView()
        {
            InitializeComponent();
            DataContext = TVSeason;
        }

        public TVSeasonPageView(TVSeasonData? tvSeason)
        {
            InitializeComponent();
            DataContext = TVSeason = tvSeason;
        }
    }
}
