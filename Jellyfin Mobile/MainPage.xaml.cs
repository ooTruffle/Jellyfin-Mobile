using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using JellyfinMobile.Models;
using System.Collections.Generic;
using JellyfinMobile.Services;

namespace JellyfinMobile
{
    public sealed partial class MainPage : Page
    {
        private JellyfinService _jellyfinService = new JellyfinService();
        private string _serverUrl, _userId, _accessToken;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _serverUrl = ServerUrlBox.Text.Trim();
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password.Trim();

            try
            {
                var loginResult = await _jellyfinService.LoginAsync(_serverUrl, username, password);
                _userId = loginResult.UserId;
                _accessToken = loginResult.AccessToken;
                await NavigateToMediaBrowser();
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Login failed: {ex.Message}";
            }
        }

        private async Task NavigateToMediaBrowser()
        {
            try
            {
                var libraries = await _jellyfinService.GetLibrariesAsync(_serverUrl, _userId, _accessToken);

                if (libraries != null && libraries.Count > 0)
                {
                    LibraryGridView.ItemsSource = libraries;
                    LoginPanel.Visibility = Visibility.Collapsed;
                    MediaBrowserPanel.Visibility = Visibility.Visible;
                    StatusBlock.Text = "";
                }
                else
                {
                    StatusBlock.Text = "No libraries found for this user/account.";
                    LoginPanel.Visibility = Visibility.Visible;
                    MediaBrowserPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Error loading libraries: {ex.Message}";
                LoginPanel.Visibility = Visibility.Visible;
                MediaBrowserPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Visible;
            MediaBrowserPanel.Visibility = Visibility.Collapsed;
            UsernameBox.Text = "";
            PasswordBox.Password = "";
            StatusBlock.Text = "";
        }

        private void LibraryGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var library = e.ClickedItem as LibraryItem;
            if (library != null)
            {
                Frame.Navigate(typeof(LibraryPage), new LibraryPageNavigationArgs
                {
                    Library = library,
                    ServerUrl = _serverUrl,
                    UserId = _userId,
                    AccessToken = _accessToken
                });
            }
        }
    }

    public class LibraryPageNavigationArgs
    {
        public LibraryItem Library { get; set; }
        public string ServerUrl { get; set; }
        public string UserId { get; set; }
        public string AccessToken { get; set; }
    }
}