using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using JellyfinMobile.Services;
using JellyfinMobile.Models;
using System.Collections.Generic;

namespace JellyfinMobile
{
    public sealed partial class MainPage : Page
    {
        private readonly JellyfinService _jellyfinService = new JellyfinService();
        private string _serverUrl;
        private string _accessToken;
        private string _userId;
        private string _currentLibraryName;
        // (UI elements...)

        public MainPage()
        {
            this.InitializeComponent();
            CreateUI();
        }

        // (UI creation methods...)

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _statusBlock.Text = "";
            _statusBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Red);

            var serverUrl = _serverUrlBox.Text.TrimEnd('/');
            var username = _usernameBox.Text;
            var password = _passwordBox.Password;

            if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _statusBlock.Text = "Please enter all fields.";
                return;
            }

            try
            {
                var loginResult = await _jellyfinService.LoginAsync(serverUrl, username, password);
                if (loginResult.Success)
                {
                    _statusBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
                    _statusBlock.Text = $"Login successful! UserId: {loginResult.UserId}";
                    _serverUrl = serverUrl;
                    _accessToken = loginResult.Token;
                    _userId = loginResult.UserId;
                    await NavigateToMediaBrowser();
                }
                else
                {
                    _statusBlock.Text = loginResult.Error ?? "Login failed.";
                }
            }
            catch (Exception ex)
            {
                _statusBlock.Text = $"Error: {ex.Message}";
            }
        }

        private async Task NavigateToMediaBrowser()
        {
            try
            {
                _loginPanel.Visibility = Visibility.Collapsed;
                _mediaBrowserScrollViewer.Visibility = Visibility.Visible;
                await LoadLibrariesFromJellyfin();
            }
            catch (Exception ex)
            {
                _statusBlock.Text = $"Error loading media browser: {ex.Message}";
            }
        }

        private async Task LoadLibrariesFromJellyfin()
        {
            if (string.IsNullOrEmpty(_serverUrl) || string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_userId))
            {
                _statusBlock.Text = "Missing authentication information";
                return;
            }

            try
            {
                var libraries = await _jellyfinService.GetLibrariesAsync(_serverUrl, _userId, _accessToken);
                _statusBlock.Text = $"Found {libraries.Count} libraries";
                _libraryGridView.ItemsSource = libraries;
            }
            catch (Exception ex)
            {
                _statusBlock.Text = $"Error fetching libraries: {ex.Message}";
            }
        }

        private async void LibraryGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var libraryItem = e.ClickedItem as MediaItem;
            if (libraryItem != null)
            {
                _currentLibraryName = libraryItem.Name;
                _statusBlock.Text = $"Loading content from: {libraryItem.Name}";
                await LoadLibraryContent(libraryItem.Id);
            }
        }

        private async Task LoadLibraryContent(string libraryId)
        {
            if (string.IsNullOrEmpty(_serverUrl) || string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_userId))
            {
                _statusBlock.Text = "Missing authentication information";
                return;
            }

            try
            {
                var mediaItems = await _jellyfinService.GetLibraryContentAsync(_serverUrl, _userId, _accessToken, libraryId);
                _statusBlock.Text = $"Found {mediaItems.Count} items in library";
                _mediaBrowserTitle.Text = _currentLibraryName;
                _libraryGridView.Visibility = Visibility.Collapsed;
                _mediaGridView.Visibility = Visibility.Visible;
                _backButton.Visibility = Visibility.Visible;
                _mediaGridView.ItemsSource = mediaItems;
                _mediaGridView.ItemTemplate = CreateMediaItemTemplate();
            }
            catch (Exception ex)
            {
                _statusBlock.Text = $"Error fetching library content: {ex.Message}";
            }
        }

        // (Other UI methods and event handlers...)
    }
}