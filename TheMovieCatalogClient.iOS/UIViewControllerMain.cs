using System.Linq;
using Foundation;
using TheMovieCatalog.Shared;
using UIKit;

namespace TheMovieCatalogClient.iOS
{
    [Register("UIViewControllerMain")]
    public class UIViewControllerMain : UIViewController
    {
        private LibVLCSharp.Platforms.iOS.VideoView _videoView;
        private LibVLCSharp.Shared.LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();

            _libVLC = new LibVLCSharp.Shared.LibVLC(enableDebugLogs: true);
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            _videoView = new LibVLCSharp.Platforms.iOS.VideoView { MediaPlayer = _mediaPlayer };
            
            View = _videoView;

            var movieLibraries = await HelperMethods.GetMovieLibrariesAsync();
            var movieStream = await HelperMethods.GetMovieStreamAsync(movieLibraries.First().MovieMetaDatas.First());
            var media = new LibVLCSharp.Shared.Media(_libVLC, new LibVLCSharp.Shared.StreamMediaInput(movieStream));
            _videoView.MediaPlayer.Play(media);
            media.Dispose();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }
    }
}