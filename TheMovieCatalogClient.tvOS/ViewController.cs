using System;
using Foundation;
using UIKit;

namespace TheMovieCatalogClient.tvOS
{
    public partial class ViewController : UIViewController
    {
        VideoView _videoView;
        LibVLCSharp.Shared.LibVLC _libVLC;
        LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}

