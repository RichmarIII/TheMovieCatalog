using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

namespace TheMovieCatalog
{
    /// <summary>
    /// Interaction logic for VideoControls.xaml
    /// </summary>
    public partial class VideoControls : UserControl, INotifyPropertyChanged
    {
        private bool _isVolumeHovered = false;
        public bool IsVolumeButtonHovered { get => _isVolumeHovered; set { _isVolumeHovered = value; NotifyPropertyChanged(nameof(IsVolumeHovered)); } }

        private bool _isVolumeSliderHovered = false;
        public bool IsVolumeSliderHovered { get => _isVolumeSliderHovered; set { _isVolumeSliderHovered = value; NotifyPropertyChanged(nameof(IsVolumeHovered)); } }

        public bool IsVolumeHovered { get => IsVolumeButtonHovered || IsVolumeSliderHovered; }

        public float VolumeLevel { get; set; }

        public bool IsFullScreen { get; private set; }

        private string _playPauseString = "Pause";

        public float MaxOpacity { get; private set; } = 0.75f;

        public string PlayPauseString
        {
            get { return _playPauseString; }
            set { _playPauseString = value; NotifyPropertyChanged(); }
        }

        public float? Position
        {
            get
            {
                var mediaPlayer = this.DataContext as LibVLCSharp.Shared.MediaPlayer;
                return mediaPlayer?.Position;
            }
            set
            {
                var mediaPlayer = this.DataContext as LibVLCSharp.Shared.MediaPlayer;
                if (mediaPlayer != null && value.HasValue)
                {
                    mediaPlayer.Position = value.Value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(MediaCurrentTime));
                }
            }
        }

        public TimeSpan? MediaCurrentTime
        {
            get
            {
                var mediaPlayer = this.DataContext as LibVLCSharp.Shared.MediaPlayer;
                if (mediaPlayer != null)
                {
                    return TimeSpan.FromMilliseconds((double)mediaPlayer.Time);
                }
                return TimeSpan.Zero;
            }
        }

        public TimeSpan? MediaLength
        {
            get
            {
                var mediaPlayer = this.DataContext as LibVLCSharp.Shared.MediaPlayer;
                if (mediaPlayer != null)
                {
                    return TimeSpan.FromMilliseconds((double)mediaPlayer.Length);
                }
                return TimeSpan.Zero;
            }
        }

        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
        private Timer _mouseMoveChecktimer;
        private Stopwatch _mouseStillStopwatch = new Stopwatch();
        private Point _lastMousePosition;

        private readonly SynchronizationContext? _syncContext;

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point Point);

        private void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        public VideoControls()
        {
            InitializeComponent();
            _syncContext = SynchronizationContext.Current;
            _playbackSlider.DataContext = this;
            _playPauseButton.DataContext = this;
            _volumeSlider.DataContext = this;
            _mediaCurrentTimeTextBox.DataContext = _mediaLengthTextBox.DataContext = this;
            _playbackSlider.ToolTipOpening += (s, a) => { _playbackSlider.ToolTip = MediaCurrentTime; };
            _mouseStillStopwatch.Start();
        }

        public void SetMediaPlayer(LibVLCSharp.Shared.MediaPlayer mediaPlayer)
        {
            this.DataContext = _mediaPlayer = mediaPlayer;

            _mouseMoveChecktimer = new Timer((a) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    Point mousePos;
                    GetCursorPos(ref mousePos);
                    if (mousePos != _lastMousePosition)
                    {
                        Visibility = Visibility.Visible;
                        _mouseStillStopwatch.Restart();
                    }
                    else
                    {
                        if (_mouseStillStopwatch.ElapsedMilliseconds > 1000)
                        {
                            Visibility = Visibility.Collapsed;
                            _mouseStillStopwatch.Restart();
                            _mouseStillStopwatch.Stop();
                        }
                    }
                    _lastMousePosition = mousePos;
                });
            }, null, 500, 500);
            
            _mediaPlayer.LengthChanged += (s, a) => { _syncContext?.Post((o) => { NotifyPropertyChanged(nameof(MediaLength)); }, null); };
            _mediaPlayer.TimeChanged += (s, a) => { _syncContext?.Post((o) => { NotifyPropertyChanged(nameof(MediaCurrentTime)); }, null); };
            _mediaPlayer.PositionChanged += (s, a) => { _syncContext?.Post((o) => { NotifyPropertyChanged(nameof(Position)); }, null); };
            _mediaPlayer.Stopped += (s, a) => { _syncContext?.Post((o) => { _mouseMoveChecktimer.Change(Timeout.Infinite, Timeout.Infinite); }, null); };
            _mediaPlayer.Paused += (s, a) => { _syncContext?.Post((o) => { _mouseMoveChecktimer.Change(Timeout.Infinite, Timeout.Infinite); }, null); };
            _mediaPlayer.Playing += (s, a) => { _syncContext?.Post((o) => { _mouseMoveChecktimer.Change(500, 500); }, null); };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void PlayPauseButton_Clicked(object sender, RoutedEventArgs e)
        {
            LibVLCSharp.Shared.MediaPlayer? mediaPlayer = this.DataContext as LibVLCSharp.Shared.MediaPlayer;
            if (mediaPlayer != null)
            {
                if (mediaPlayer.IsPlaying)
                {
                    mediaPlayer.Pause();
                    PlayPauseString = "Play";
                }
                else
                {
                    mediaPlayer.Play();
                    PlayPauseString = "Pause";
                }
            }
        }

        private void PreviousChapterButton_Clicked(object sender, RoutedEventArgs e)
        {
            LibVLCSharp.Shared.MediaPlayer? mediaPlayer = this.DataContext as LibVLCSharp.Shared.MediaPlayer;
            if (mediaPlayer != null)
            {
                mediaPlayer.PreviousChapter();
            }
        }

        private void NextChapterButton_Clicked(object sender, RoutedEventArgs e)
        {
            LibVLCSharp.Shared.MediaPlayer? mediaPlayer = this.DataContext as LibVLCSharp.Shared.MediaPlayer;
            if (mediaPlayer != null)
            {
                mediaPlayer.NextChapter();
            }
        }

        private void FullScreenButton_Clicked(object sender, RoutedEventArgs e)
        {
            LibVLCSharp.Shared.MediaPlayer? mediaPlayer = this.DataContext as LibVLCSharp.Shared.MediaPlayer;
            if (mediaPlayer != null)
            {
                if (App.Instance != null)
                {
                    if (IsFullScreen)
                    {
                        App.Instance.MainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                        App.Instance.MainWindow.WindowState = WindowState.Normal;
                        App.Instance.MainWindow.ResizeMode = ResizeMode.CanResize;
                        App.Instance.MainWindow.Topmost = false;
                        App.Instance.MainWindow.ShowInTaskbar = true;
                        IsFullScreen = false;
                    }
                    else
                    {
                        App.Instance.MainWindow.WindowStyle = WindowStyle.None;
                        App.Instance.MainWindow.WindowState = WindowState.Maximized;
                        App.Instance.MainWindow.ResizeMode = ResizeMode.NoResize;
                        App.Instance.MainWindow.Topmost = true;
                        App.Instance.MainWindow.ShowInTaskbar = false;
                        IsFullScreen = true;
                    }
                }
            }
        }
        
        private void VolumeButton_MouseEnter(object sender, MouseEventArgs e)
        {
            IsVolumeButtonHovered = true;
        }

        private void VolumeButton_MouseLeave(object sender, MouseEventArgs e)
        {
           
        }

        private void VolumeSlider_MouseEnter(object sender, MouseEventArgs e)
        {
            IsVolumeSliderHovered = true;
        }

        private void VolumeSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            
        }

        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            IsVolumeSliderHovered = false;
            IsVolumeButtonHovered = false;
        }
    }
}
