using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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

        /// <summary>
        /// Play media from a given URI.
        /// </summary>
        public void PlayMedia(Uri mediaUri)
        {
            if (mediaUri == null) throw new ArgumentNullException(nameof(mediaUri));
            PlayerMediaElement.Source = mediaUri;
            PlayerMediaElement.Play();
            this.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Stop playback and close player.
        /// </summary>
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