using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using TheMovieCatalog.ExtensionMethods;
using TheMovieCatalog.Shared.ExtensionMethods;

namespace TheMovieCatalog
{
    public class CustomSlider : Slider
    {
        public object ToolTipContent
        {
            get { return (object)GetValue(ToolTipContentProperty); }
            set { SetValue(ToolTipContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ToolTipContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ToolTipContentProperty =
            DependencyProperty.Register("ToolTipContent", typeof(object), typeof(CustomSlider), new PropertyMetadata(null));

        public Point ToolTipOffset
        {
            get { return (Point)GetValue(ToolTipOffsetProperty); }
            set { SetValue(ToolTipOffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ToolTipOffset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ToolTipOffsetProperty =
            DependencyProperty.Register("ToolTipOffset", typeof(Point), typeof(CustomSlider), new PropertyMetadata(new Point(0, 0)));



        private Window? _window;
        public CustomSlider()
        {
        }

        protected override void OnThumbDragStarted(DragStartedEventArgs e)
        {
            base.OnThumbDragStarted(e);

            var thumb = this.GetChildOfType<Thumb>();

            var thumbPointLocal = thumb.PointToScreen(new Point(0, 0));
            thumbPointLocal.X /= 2.0f;
            thumbPointLocal.Y /= 2.0f;
            thumbPointLocal.X += ToolTipOffset.X;
            thumbPointLocal.Y += ToolTipOffset.Y;

            if (_window != null)
                _window.Close();

            var contentPresenter = new ContentPresenter();
            contentPresenter.Content = ToolTipContent;
            contentPresenter.HorizontalAlignment = HorizontalAlignment.Center;
            contentPresenter.VerticalAlignment = VerticalAlignment.Center;
            contentPresenter.SetBinding(ContentPresenter.ContentProperty, new Binding(nameof(ToolTipContent)) { Mode = BindingMode.OneWay, Source = this });
            _window = new Window();
            _window.WindowState = WindowState.Normal;
            _window.WindowStyle = WindowStyle.None;
            _window.ResizeMode = ResizeMode.NoResize;
            _window.Topmost = true;
            _window.SizeToContent = SizeToContent.WidthAndHeight;
            _window.FontSize = FontSize;
            _window.Content = contentPresenter;
            _window.Show();
            _window.Left = thumbPointLocal.X;
            _window.Top = thumbPointLocal.Y - _window.Height;
        }

        protected override void OnThumbDragDelta(DragDeltaEventArgs e)
        {
            base.OnThumbDragDelta(e);

            var thumb = this.GetChildOfType<Thumb>();
            var thumbPointLocal = thumb.PointToScreen(new Point(0, 0));
            thumbPointLocal.X /= 2.0f;
            thumbPointLocal.Y /= 2.0f;
            thumbPointLocal.X += ToolTipOffset.X;
            thumbPointLocal.Y += ToolTipOffset.Y;

            if (_window != null)
            {
                _window.Left = thumbPointLocal.X;
                _window.Top = thumbPointLocal.Y - _window.Height;
            }
        }

        protected override void OnThumbDragCompleted(DragCompletedEventArgs e)
        {
            base.OnThumbDragCompleted(e);
            if (_window != null)
                _window.Close();
        }
    }
}
