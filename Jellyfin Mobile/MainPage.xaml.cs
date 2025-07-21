using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using JellyfinMobile.Models;
using JellyfinMobile.Services;
using JellyfinMobile.Controls;

namespace JellyfinMobile
{
    public sealed partial class MainPage : Page
    {
        private readonly JellyfinService _jellyfinService = new JellyfinService();
        private string _serverUrl;
        private string _accessToken;
        private string _userId;
        private string _currentLibraryName;
        private string _currentLibraryType;
        private MediaItem _selectedMedia;
        private List<MediaItem> _seasons = new List<MediaItem>();
        private string _lastPlaybackInfoRaw = "";

        // Dialog guard
        private bool _dialogOpen = false;

        public MainPage()
        {
            this.InitializeComponent();
            BackButton.Click += BackButton_Click;
            LibraryGridView.ItemClick += LibraryGridView_ItemClick;
            MediaGridView.ItemClick += MediaGridView_ItemClick;
            PlayButton.Click += PlayButton_Click;
            EpisodeGridView.ItemClick += EpisodeGridView_ItemClick;
            SeasonPivot.SelectionChanged += SeasonPivot_SelectionChanged;
            SeasonGridView.ItemClick += SeasonGridView_ItemClick;
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SharedMediaPlayer.MediaClosed += SharedMediaPlayer_MediaClosed;
            SharedMediaPlayer.MediaFailedEvent += SharedMediaPlayer_MediaFailedEvent;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            StatusBlock.Text = "";
            StatusBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);

            var serverUrl = ServerUrlBox.Text.TrimEnd('/');
            var username = UsernameBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                StatusBlock.Text = "Please enter all fields.";
                return;
            }

            try
            {
                var loginResult = await _jellyfinService.LoginAsync(serverUrl, username, password);
                if (loginResult.Success)
                {
                    StatusBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
                    StatusBlock.Text = $"Login successful! UserId: {loginResult.UserId}";
                    _serverUrl = serverUrl;
                    _accessToken = loginResult.Token;
                    _userId = loginResult.UserId;
                    LoginPanel.Visibility = Visibility.Collapsed;
                    MediaBrowserPanel.Visibility = Visibility.Visible;
                    await LoadLibrariesFromJellyfin();
                }
                else
                {
                    StatusBlock.Text = loginResult.Error ?? "Login failed.";
                }
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Error: {ex.Message}";
            }
        }

        private void SeasonGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var seasonItem = e.ClickedItem as MediaItem;
            if (seasonItem != null)
            {
                _ = ShowEpisodesForSeason(seasonItem);
            }
        }

        private async Task LoadLibrariesFromJellyfin()
        {
            LibraryGridView.Visibility = Visibility.Visible;
            MediaGridView.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Collapsed;
            MediaBrowserTitle.Text = "Libraries";
            MediaDetailsPanel.Visibility = Visibility.Collapsed;
            SeasonPivot.Visibility = Visibility.Collapsed;
            EpisodeGridView.Visibility = Visibility.Collapsed;
            EpisodeListTitle.Visibility = Visibility.Collapsed;
            PlayButton.Visibility = Visibility.Collapsed;
            EpisodeDetailPanel.Visibility = Visibility.Collapsed;
            SharedMediaPlayer.Visibility = Visibility.Collapsed;

            var libraries = await _jellyfinService.GetLibrariesAsync(_serverUrl, _userId, _accessToken);
            LibraryGridView.ItemsSource = libraries;
        }

        private async void LibraryGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var libraryItem = e.ClickedItem as MediaItem;
            if (libraryItem != null)
            {
                _currentLibraryName = libraryItem.Name;
                _currentLibraryType = libraryItem.Type;
                MediaBrowserTitle.Text = libraryItem.Name;
                await LoadLibraryContent(libraryItem.Id, libraryItem.Type);
            }
        }

        private async Task LoadLibraryContent(string libraryId, string libraryType)
        {
            var mediaItems = await _jellyfinService.GetLibraryContentAsync(_serverUrl, _userId, _accessToken, libraryId);
            MediaGridView.ItemsSource = mediaItems;
            MediaGridView.Visibility = Visibility.Visible;
            LibraryGridView.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Visible;
            MediaDetailsPanel.Visibility = Visibility.Collapsed;
            SeasonPivot.Visibility = Visibility.Collapsed;
            EpisodeGridView.Visibility = Visibility.Collapsed;
            EpisodeListTitle.Visibility = Visibility.Collapsed;
            PlayButton.Visibility = Visibility.Collapsed;
            EpisodeDetailPanel.Visibility = Visibility.Collapsed;
            SharedMediaPlayer.Visibility = Visibility.Collapsed;
        }

        private async void MediaGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var mediaItem = e.ClickedItem as MediaItem;
            if (mediaItem != null && (mediaItem.Type == "Series" || mediaItem.Type == "Show"))
            {
                MediaGridView.Visibility = Visibility.Collapsed;
                _selectedMedia = mediaItem;
                SelectedMediaTitle.Text = mediaItem.Name;
                MediaDetailsPanel.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Collapsed;
                SeasonPivot.Visibility = Visibility.Collapsed;
                EpisodeGridView.Visibility = Visibility.Collapsed;
                EpisodeListTitle.Visibility = Visibility.Collapsed;
                EpisodeDetailPanel.Visibility = Visibility.Collapsed;
                SharedMediaPlayer.Visibility = Visibility.Collapsed;

                MediaBrowserTitle.Text = mediaItem.Name;
                _seasons = await _jellyfinService.GetSeasonsAsync(_serverUrl, _userId, _accessToken, mediaItem.Id);
                SeasonPivot.Items.Clear();
                foreach (var season in _seasons)
                {
                    var pivotItem = new PivotItem
                    {
                        Header = season.Name,
                        Tag = season
                    };
                    SeasonPivot.Items.Add(pivotItem);
                }
                if (_seasons.Count > 0)
                {
                    SeasonPivot.SelectedIndex = 0;
                    SeasonPivot.Visibility = Visibility.Visible;
                    await ShowEpisodesForSeason(_seasons[0]);
                }
            }
            else if (mediaItem != null && mediaItem.Type == "Movie")
            {
                MediaGridView.Visibility = Visibility.Collapsed;
                _selectedMedia = mediaItem;
                SelectedMediaTitle.Text = mediaItem.Name;
                MediaDetailsPanel.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Visible;
                SeasonPivot.Visibility = Visibility.Collapsed;
                EpisodeGridView.Visibility = Visibility.Collapsed;
                EpisodeListTitle.Visibility = Visibility.Collapsed;
                EpisodeDetailPanel.Visibility = Visibility.Collapsed;
                SharedMediaPlayer.Visibility = Visibility.Collapsed;
                MediaBrowserTitle.Text = mediaItem.Name;
            }
        }

        private async void SeasonPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SeasonPivot.SelectedItem is PivotItem pivotItem && pivotItem.Tag is MediaItem season)
            {
                await ShowEpisodesForSeason(season);
            }
        }

        private async Task ShowEpisodesForSeason(MediaItem season)
        {
            EpisodeListTitle.Visibility = Visibility.Visible;
            EpisodeDetailPanel.Visibility = Visibility.Collapsed;
            SharedMediaPlayer.Visibility = Visibility.Collapsed;
            var episodes = await _jellyfinService.GetEpisodesAsync(_serverUrl, _userId, _accessToken, season.Id);
            EpisodeGridView.ItemsSource = episodes;
            EpisodeGridView.Visibility = Visibility.Visible;
        }

        public async Task PlayMediaAsync(string itemId)
        {
            var playbackResult = await _jellyfinService.GetPlayableUrlAndRawAsync(_serverUrl, _accessToken, itemId, _userId);
            _lastPlaybackInfoRaw = playbackResult.RawJson;

            if (!string.IsNullOrEmpty(playbackResult.Url) &&
                (playbackResult.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                 playbackResult.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                SharedMediaPlayer.Visibility = Visibility.Visible;
                SharedMediaPlayer.PlayMedia(new Uri(playbackResult.Url));
            }
            else
            {
                await ShowVerbosePlaybackErrorAsync(itemId, _lastPlaybackInfoRaw);
            }
        }

        private async Task ShowVerbosePlaybackErrorAsync(string itemId, string rawJellyfinInfo)
        {
            if (_dialogOpen) return;
            _dialogOpen = true;

            string errorDetails = $"This video cannot be played.\n\n" +
                $"Item ID: {itemId}\n" +
                $"Jellyfin PlaybackInfo:\n{rawJellyfinInfo}";

            var dialog = new ContentDialog
            {
                Title = "Playback Error",
                Content = errorDetails,
                CloseButtonText = "Close"
            };
            await dialog.ShowAsync();
            _dialogOpen = false;
        }

        private async void SharedMediaPlayer_MediaFailedEvent(object sender, ExceptionRoutedEventArgs e)
        {
            await ShowVerbosePlaybackErrorAsync(_selectedMedia?.Id, _lastPlaybackInfoRaw);
        }

        private void SharedMediaPlayer_MediaClosed(object sender, EventArgs e)
        {
            SharedMediaPlayer.Visibility = Visibility.Collapsed;
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMedia != null)
                await PlayMediaAsync(_selectedMedia.Id);
        }

        private void EpisodeGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var episodeItem = e.ClickedItem as MediaItem;
            if (episodeItem != null)
            {
                var navParams = new EpisodePageParams
                {
                    Episode = episodeItem,
                    ServerUrl = _serverUrl,
                    UserId = _userId,
                    AccessToken = _accessToken
                };
                Frame.Navigate(typeof(EpisodePage), navParams);
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedMediaPlayer.Visibility == Visibility.Visible)
            {
                SharedMediaPlayer.ClosePlayer();
                return;
            }
            if (MediaDetailsPanel.Visibility == Visibility.Visible)
            {
                MediaDetailsPanel.Visibility = Visibility.Collapsed;
                MediaGridView.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
                MediaBrowserTitle.Text = _currentLibraryName;
            }
            else if (MediaGridView.Visibility == Visibility.Visible)
            {
                await LoadLibrariesFromJellyfin();
            }
        }
    }
}