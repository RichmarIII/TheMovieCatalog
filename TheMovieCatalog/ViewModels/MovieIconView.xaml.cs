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
    public partial class MovieIconView : UserControl, INotifyPropertyChanged
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

        public MovieIconView()
        {
            this.InitializeComponent();
            _watchButton.DataContext = this;
            _watchButton.SetBinding(Button.VisibilityProperty, new Binding() { Mode = BindingMode.TwoWay, Path = new PropertyPath("IsHovered"), Source = this });
        }

        public void WatchButton_Tapped(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            
            App.PlayMedia(this.DataContext as MediaData);
        }

        private void Image_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IsHovered = Visibility.Visible;
        }

        private void Image_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IsHovered = Visibility.Collapsed ;
        }

        private void Image_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            App.ShowMovie(this.DataContext as MovieData);
        }
    }
}
