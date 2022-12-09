using MediaManager.Platforms.Ios.Video;
using System;
using System.Diagnostics;
using System.Linq;
using TheMovieCatalog.Shared;
using UIKit;

namespace TheMovieCatalogClient.tvOS
{
    public partial class ViewController : UIViewController
    {
        private VideoView? videoView;
        private LibVLCSharp.Shared.MediaPlayer? mediaPlayer;
        private LibVLCSharp.Shared.LibVLC? libVLC;
        private LibVLCSharp.Shared.Media? media;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override async void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            libVLC = new LibVLCSharp.Shared.LibVLC(enableDebugLogs: true);
            libVLC.Log += LibVLC_Log;
            
            mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(libVLC);
            media = new LibVLCSharp.Shared.Media(libVLC, new LibVLCSharp.Shared.StreamMediaInput(await HelperMethods.GetMovieStreamAsync((await HelperMethods.GetMovieLibrariesAsync()).First().MovieMetaDatas.First())));
            mediaPlayer.Media = media;
            videoView = new VideoView();
            View.AddSubview(videoView);
            videoView.Frame = View.Bounds;
            mediaPlayer.Play();
        }

        private void LibVLC_Log(object sender, LibVLCSharp.Shared.LogEventArgs e)
        {
            Debug.WriteLine(e.Message);
        }

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}

