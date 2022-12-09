using AVFoundation;
using LibVLCSharp.Shared;
using MediaManager;
using MediaManager.Platforms.Ios.Video;
using System;
using System.Drawing.Imaging;
using System.Linq;
using TheMovieCatalog.Shared;
using UIKit;

namespace TheMovieCatalogClient.tvOS
{
    public partial class ViewController : UIViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
            CrossMediaManager.Current.MediaPlayer.AutoAttachVideoView = false;
            var playerView = new VideoView();
            View.AddSubview(playerView);
            CrossMediaManager.Current.MediaPlayer.VideoView = playerView;
        }


        public override async void ViewDidLoad()
        {
            //AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);
            base.ViewDidLoad();

            var movieLibraries = await HelperMethods.GetMovieLibrariesAsync();
            var movieStream = await HelperMethods.GetMovieStreamAsync(movieLibraries.First().MovieMetaDatas.First());
            await CrossMediaManager.Current.Play("https://www.appsloveworld.com/wp-content/uploads/2018/10/Sample-Videos-Mp425.mp4");
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

