using CoreFoundation;
using Foundation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using TheMovieCatalog.Shared;
using TheMovieCatalog.WebAPI.DataModels;
using UIKit;
using Xamarin.Essentials;

namespace TheMovieCatalogClient.iOS
{
    [Register("UIViewControllerMain")]
    public class UIViewControllerMain : UIViewController
    {
        LibVLCSharp.Platforms.iOS.VideoView _videoView;
        LibVLCSharp.Shared.LibVLC _libVLC;
        LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

        async public override void ViewDidLoad()
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