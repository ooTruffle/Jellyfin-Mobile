using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using JellyfinMobile.Models;
using JellyfinMobile.Services;
using JellyfinMobile.Controls; // Import your control's namespace

namespace JellyfinMobile
{
    public sealed partial class EpisodePage : Page
    {
        private MediaItem _episode;
        private string _serverUrl;
        private string _userId;
        private string _accessToken;
        private JellyfinService _jellyfinService = new JellyfinService();

        public EpisodePage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var navParams = e.Parameter as EpisodePageParams;
            if (navParams != null)
            {
                _episode = navParams.Episode;
                _serverUrl = navParams.ServerUrl;
                _userId = navParams.UserId;
                _accessToken = navParams.AccessToken;
            }

            if (_episode != null)
            {
                EpisodeTitle.Text = _episode.Name;
                var episodeDetails = await _jellyfinService.GetEpisodeDetailsAsync(_serverUrl, _userId, _accessToken, _episode.Id);
                if (!string.IsNullOrEmpty(episodeDetails?.Overview))
                    EpisodeOverview.Text = episodeDetails.Overview;
                else
                    EpisodeOverview.Text = "No overview available.";
                EpisodeImage.Source = new BitmapImage(new Uri(_episode.ImageUrl ?? "ms-appx:///Assets/placeholder.png"));
                PlayButton.IsEnabled = true;
            }
            else
            {
                EpisodeTitle.Text = "Episode not found";
                EpisodeOverview.Text = "";
                PlayButton.IsEnabled = false;
            }
        }

        private bool _dialogOpen = false;
        private async Task ShowVerbosePlaybackErrorAsync(string episodeId, string rawJellyfinInfo)
        {
            if (_dialogOpen) return;
            _dialogOpen = true;

            string errorDetails = $"This episode cannot be played.\n\n" +
                $"Episode ID: {episodeId}\n" +
                $"Jellyfin PlaybackInfo:\n{rawJellyfinInfo}";

            var dialog = new ContentDialog()
            {
                Title = "Playback Error",
                Content = errorDetails,
                CloseButtonText = "Close"
            };
            await dialog.ShowAsync();
            _dialogOpen = false;
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButton.IsEnabled = false;
            if (_episode != null)
            {
                var playbackResult = await _jellyfinService.GetPlayableUrlAndRawAsync(_serverUrl, _accessToken, _episode.Id, _userId);

                if (!string.IsNullOrEmpty(playbackResult.Url))
                {
                    SharedMediaPlayer.Visibility = Visibility.Visible;
                    SharedMediaPlayer.PlayMedia(new Uri(playbackResult.Url));
                }
                else
                {
                    await ShowVerbosePlaybackErrorAsync(_episode.Id, playbackResult.RawJson);
                }
            }
            PlayButton.IsEnabled = true;
        }

        private void SharedMediaPlayer_MediaClosed(object sender, EventArgs e)
        {
            SharedMediaPlayer.Visibility = Visibility.Collapsed;
        }

        private async void SharedMediaPlayer_MediaFailedEvent(object sender, ExceptionRoutedEventArgs e)
        {
            await ShowVerbosePlaybackErrorAsync(_episode?.Id, "Media playback failed.");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SharedMediaPlayer.MediaClosed += SharedMediaPlayer_MediaClosed;
            SharedMediaPlayer.MediaFailedEvent += SharedMediaPlayer_MediaFailedEvent;
        }

        // Back button goes to episode list
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}