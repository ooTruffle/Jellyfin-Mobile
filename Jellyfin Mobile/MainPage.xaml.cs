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

        public MainPage()
        {
            this.InitializeComponent();
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
                    await NavigateToMediaBrowser();
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

        private async Task NavigateToMediaBrowser()
        {
            try
            {
                // Hide login UI, show media browser UI
                // You may need to add named panels in your XAML for this!
                // Example:
                // LoginPanel.Visibility = Visibility.Collapsed;
                // MediaBrowserPanel.Visibility = Visibility.Visible;
                await LoadLibrariesFromJellyfin();
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Error loading media browser: {ex.Message}";
            }
        }

        private async Task LoadLibrariesFromJellyfin()
        {
            if (string.IsNullOrEmpty(_serverUrl) || string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_userId))
            {
                StatusBlock.Text = "Missing authentication information";
                return;
            }

            try
            {
                var libraries = await _jellyfinService.GetLibrariesAsync(_serverUrl, _userId, _accessToken);
                StatusBlock.Text = $"Found {libraries.Count} libraries";
                // LibraryGridView.ItemsSource = libraries; // Uncomment if you have LibraryGridView in XAML
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Error fetching libraries: {ex.Message}";
            }
        }

        private async void LibraryGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var libraryItem = e.ClickedItem as MediaItem;
            if (libraryItem != null)
            {
                _currentLibraryName = libraryItem.Name;
                StatusBlock.Text = $"Loading content from: {libraryItem.Name}";
                await LoadLibraryContent(libraryItem.Id);
            }
        }

        private async Task LoadLibraryContent(string libraryId)
        {
            if (string.IsNullOrEmpty(_serverUrl) || string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_userId))
            {
                StatusBlock.Text = "Missing authentication information";
                return;
            }

            try
            {
                var mediaItems = await _jellyfinService.GetLibraryContentAsync(_serverUrl, _userId, _accessToken, libraryId);
                StatusBlock.Text = $"Found {mediaItems.Count} items in library";
                // MediaBrowserTitle.Text = _currentLibraryName; // Uncomment if you have MediaBrowserTitle in XAML
                // LibraryGridView.Visibility = Visibility.Collapsed;
                // MediaGridView.Visibility = Visibility.Visible;
                // BackButton.Visibility = Visibility.Visible;
                // MediaGridView.ItemsSource = mediaItems;
                // MediaGridView.ItemTemplate = CreateMediaItemTemplate(); // If you have a template method
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Error fetching library content: {ex.Message}";
            }
        }
    }
}