using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for TVSeriesIconView.xaml
    /// </summary>
    public partial class TVSeriesIconView : UserControl, INotifyPropertyChanged
    {
        public TVSeriesIconView()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            App.Instance.ShowTVSeries(this.DataContext as TVSeriesData);
        }
    }
}
