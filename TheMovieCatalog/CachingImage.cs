using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace TheMovieCatalog
{
    public class CachingImage
    {
        private readonly Uri _uri;
        public CachingImage(string uriString)
        {
            _uri = new Uri(uriString, UriKind.RelativeOrAbsolute);
        }

        private BitmapImage _image;

        public ImageSource Image
        {
            get
            {
                if (_image == null)
                {
                    _image = new BitmapImage(_uri);
                    _image.DownloadCompleted += (sender, args) => ((BitmapImage)sender).Freeze();
                }

                return _image;
            }
        }
    }
}
