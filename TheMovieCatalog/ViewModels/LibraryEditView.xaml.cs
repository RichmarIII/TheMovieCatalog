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
    public sealed partial class LibraryEditView : UserControl, INotifyPropertyChanged
    {
        private MediaLibraryData _mediaLibraryData = new MediaLibraryData();
        public MediaLibraryData MediaLibraryData { get { return _mediaLibraryData; } set { _mediaLibraryData = value; NotifyPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;

        public LibraryEditView()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        public LibraryEditView(MediaLibraryData mediaLibraryData)
        {
            this.InitializeComponent();
            this.DataContext = this;
            MediaLibraryData = mediaLibraryData;
            _mediaFolderTextBlock.SetBinding(TextBlock.TextProperty, new Binding() { Mode = BindingMode.OneWay, Path = new PropertyPath("MediaLibraryData.MediaFolder"), Converter = new StorageFolderToStringConverter(), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        }

        private void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        private void LibraryFolderButton_Tapped(object sender, RoutedEventArgs e)
        {
            var MediaFolderPicker = new System.Windows.Forms.FolderBrowserDialog();
            MediaFolderPicker.AutoUpgradeEnabled = true;
            MediaFolderPicker.ShowDialog();
            MediaLibraryData.MediaFolder = new DirectoryInfo(MediaFolderPicker.SelectedPath);
            MediaLibraryData.MediaFolderString = MediaLibraryData.MediaFolder.FullName;
        }

        private void Icon_Tapped(object sender, RoutedEventArgs e)
        {
            var LibraryIconFilePicker = new System.Windows.Forms.OpenFileDialog();
            LibraryIconFilePicker.AddExtension = true;
            LibraryIconFilePicker.AutoUpgradeEnabled = true;
            LibraryIconFilePicker.CheckFileExists = true;
            LibraryIconFilePicker.CheckPathExists = true;
            LibraryIconFilePicker.Multiselect = false;
            LibraryIconFilePicker.SupportMultiDottedExtensions = true;
            LibraryIconFilePicker.Filter = "Image Files (*.bmp, *.jpg, *.jpeg, *.png, *.tif, *.tiff)|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff";
            LibraryIconFilePicker.ShowDialog();
            MediaLibraryData.IconImagePath = LibraryIconFilePicker.FileName;
        }

        private void AddButton_Clicked(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.DialogResult = true;
            window.Close();
        }
    }
}
