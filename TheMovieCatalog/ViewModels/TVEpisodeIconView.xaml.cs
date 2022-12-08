using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TheMovieCatalog.DataModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TheMovieCatalog.ViewModels
{

    public sealed partial class TVEpisodeIconView : UserControl, INotifyPropertyChanged
    {
        private Visibility _isHovered = Visibility.Collapsed;
        public Visibility IsHovered
        {
            get { return _isHovered; }
            set { _isHovered = value; NotifyPropertyChanged(); }
        }

        public Button WatchButton
        {
            get { return _watchButton; }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public TVEpisodeIconView()
        {
            this.InitializeComponent();
            _watchButton.DataContext = this;
            _watchButton.SetBinding(Button.VisibilityProperty, new Binding() { Mode = BindingMode.TwoWay, Path = new PropertyPath("IsHovered"), Source = this });
        }

        public void WatchButton_Tapped(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            App.PlayMedia(DataContext as MediaData);
        }

        public void Image_MouseEnter(object sender, RoutedEventArgs e)
        {
            IsHovered = Visibility.Visible;
        }

        public void Image_MouseLeave(object sender, RoutedEventArgs e)
        {
            IsHovered = Visibility.Collapsed;
        }

        private void OnMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.Frame.Navigate(new TVEpisodePageView(DataContext as TVEpisodeData));
        }
    }
}
