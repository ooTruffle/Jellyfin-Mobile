using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System;

namespace JellyfinMobile.Controls
{
    public sealed partial class MediaPlayerControl : UserControl
    {
        public MediaPlayerControl()
        {
            this.InitializeComponent();
            PlayerMediaElement.MediaFailed += PlayerMediaElement_MediaFailed;
        }

        public event EventHandler MediaClosed;
        public event EventHandler<ExceptionRoutedEventArgs> MediaFailedEvent;

        public void PlayMedia(Uri mediaUri)
        {
            PlayerMediaElement.Source = mediaUri;
            PlayerMediaElement.Play();
            this.Visibility = Visibility.Visible;
        }

        public void ClosePlayer()
        {
            PlayerMediaElement.Stop();
            PlayerMediaElement.Source = null;
            this.Visibility = Visibility.Collapsed;

            MediaClosed?.Invoke(this, EventArgs.Empty);
        }

        private void PlayerMediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MediaFailedEvent?.Invoke(this, e);
        }

        private void ClosePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            ClosePlayer();
        }
    }
}