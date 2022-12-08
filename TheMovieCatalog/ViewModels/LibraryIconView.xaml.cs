using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TheMovieCatalog.DataModels;

namespace TheMovieCatalog.ViewModels
{
    public sealed partial class LibraryIconView : UserControl
    {
        public LibraryIconView()
        {
            this.InitializeComponent();
        }

        private void SelectButton_Clicked(object sender, RoutedEventArgs e)
        {
            var mediaLibraryData = this.DataContext as MediaLibraryData;
            if (mediaLibraryData != null)
            {
                MainWindow.Instance?.ShowMediaLibrary(mediaLibraryData);
            }
        }

        private async void RefreshButton_Clicked(object sender, RoutedEventArgs e)
        {
            var mediaLibraryData = this.DataContext as MediaLibraryData;
            if (mediaLibraryData != null)
            {
                await mediaLibraryData.ScanFiles();
                await mediaLibraryData.GenerateMetadata(true);
                mediaLibraryData.Save();

                MainWindow.Instance?.ShowMediaLibrary(mediaLibraryData);
            }
        }

        private async void RegenerateContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var mediaLibraryData = this.DataContext as MediaLibraryData;
            if (mediaLibraryData != null)
            {
                await mediaLibraryData.ScanFiles();
                await mediaLibraryData.GenerateMetadata(false);
                mediaLibraryData.Save();

                MainWindow.Instance?.ShowMediaLibrary(mediaLibraryData);
            }
        }
    }
}
