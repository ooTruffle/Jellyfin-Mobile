using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using JellyfinMobile.Models;
using JellyfinMobile.Services;

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

        public MainPage()
        {
            this.InitializeComponent();
            BackButton.Click += BackButton_Click;
            LibraryGridView.ItemClick += LibraryGridView_ItemClick;
            MediaGridView.ItemClick += MediaGridView_ItemClick;
            PlayButton.Click += PlayButton_Click;
            EpisodeGridView.ItemClick += EpisodeGridView_ItemClick;
            SeasonPivot.SelectionChanged += SeasonPivot_SelectionChanged;
            PlayerMediaElement.MediaFailed += PlayerMediaElement_MediaFailed;
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
            PlayerGrid.Visibility = Visibility.Collapsed;

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
            PlayerGrid.Visibility = Visibility.Collapsed;
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
                PlayerGrid.Visibility = Visibility.Collapsed;

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
                PlayerGrid.Visibility = Visibility.Collapsed;
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
            PlayerGrid.Visibility = Visibility.Collapsed;
            var episodes = await _jellyfinService.GetEpisodesAsync(_serverUrl, _userId, _accessToken, season.Id);
            EpisodeGridView.ItemsSource = episodes;
            EpisodeGridView.Visibility = Visibility.Visible;
        }

        // FULL SCREEN PLAYER LOGIC
        private async Task PlayMediaAsync(string itemId)
        {
            var playbackResult = await _jellyfinService.GetPlayableUrlAndRawAsync(_serverUrl, _accessToken, itemId, _userId);
            _lastPlaybackInfoRaw = playbackResult.RawJson;

            if (!string.IsNullOrEmpty(playbackResult.Url))
            {
                PlayerGrid.Visibility = Visibility.Visible;
                PlayerMediaElement.Source = new Uri(playbackResult.Url);
                PlayerMediaElement.Play();
            }
            else
            {
                ShowPlaybackError(_lastPlaybackInfoRaw);
            }
        }

        private void ClosePlayer_Click(object sender, RoutedEventArgs e)
        {
            PlayerMediaElement.Stop();
            PlayerMediaElement.Source = null;
            PlayerGrid.Visibility = Visibility.Collapsed;
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMedia != null)
                await PlayMediaAsync(_selectedMedia.Id);
        }

        private async void EpisodePlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (EpisodeDetailPanel.Tag is MediaItem episode)
                await PlayMediaAsync(episode.Id);
        }

        private void EpisodeGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var episodeItem = e.ClickedItem as MediaItem;
            if (episodeItem != null)
            {
                EpisodeDetailPanel.Visibility = Visibility.Visible;
                EpisodeDetailPanel.Tag = episodeItem;
                EpisodeDetailTitle.Text = episodeItem.Name;
                EpisodeDetailOverview.Text = episodeItem.Overview ?? "No overview available.";
                EpisodeDetailImage.Source = new BitmapImage(new Uri(episodeItem.ImageUrl ?? "ms-appx:///Assets/placeholder.png"));
                EpisodePlayButton.Click -= EpisodePlayButton_Click;
                EpisodePlayButton.Click += EpisodePlayButton_Click;
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerGrid.Visibility == Visibility.Visible)
            {
                ClosePlayer_Click(sender, e);
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

        private void PlayerMediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ShowPlaybackError(_lastPlaybackInfoRaw);
        }

        private void ShowPlaybackError(string jellyfinRaw)
        {
            PlayerGrid.Visibility = Visibility.Visible;
            // Show error overlay (or use a TextBlock overlaying the player)
            // For simplicity, use a MessageDialog (or you can overlay a TextBlock in XAML over the player)
            var dialog = new ContentDialog()
            {
                Title = "Playback Error",
                Content = $"Unsupported video type or invalid file path.\n\nJellyfin PlaybackInfo:\n{jellyfinRaw}",
                CloseButtonText = "Close"
            };
            _ = dialog.ShowAsync();
        }
    }
}