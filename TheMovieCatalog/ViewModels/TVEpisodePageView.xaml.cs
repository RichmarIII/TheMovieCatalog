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
    /// Interaction logic for TVEpisodePageView.xaml
    /// </summary>
    public partial class TVEpisodePageView : Page
    {
        public TVEpisodeData? TVEpisode { get; private set; }
        
        public TVEpisodePageView()
        {
            InitializeComponent();
        }

        public TVEpisodePageView(TVEpisodeData? tvEpisode)
        {
            InitializeComponent();
            this.DataContext = this.TVEpisode = tvEpisode;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            TheaterPageView.Instance.PlayMedia(TVEpisode);
        }
    }
}
