using Foundation;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

namespace TheMovieCatalogClient.tvOS
{
    //
    // Summary:
    //     VideoView implementation for the Apple platform
    public class VideoView : UIView, IVideoView
    {
        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;

        //
        // Summary:
        //     The MediaPlayer object attached to this VideoView. Use this to manage playback
        //     and more
        public LibVLCSharp.Shared.MediaPlayer? MediaPlayer
        {
            get
            {
                return _mediaPlayer;
            }
            set
            {
                if (_mediaPlayer != value)
                {
                    Detach();
                    _mediaPlayer = value;
                    if (_mediaPlayer != null)
                    {
                        Attach();
                    }
                }
            }
        }

        private void Attach()
        {
            if (MediaPlayer != null && MediaPlayer!.NativeReference != IntPtr.Zero)
            {
                MediaPlayer!.NsObject = base.Handle;
            }
        }

        private void Detach()
        {
            if (MediaPlayer != null && MediaPlayer!.NativeReference != IntPtr.Zero)
            {
                MediaPlayer!.NsObject = IntPtr.Zero;
            }
        }

        //
        // Summary:
        //     Detach the mediaplayer from the view and dispose the view
        //
        // Parameters:
        //   disposing:
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Detach();
        }
    }
}