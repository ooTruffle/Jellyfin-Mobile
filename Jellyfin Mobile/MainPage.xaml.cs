using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace JellyfinMobile
{
    public sealed partial class MainPage : Page
    {
        private string _serverUrl;
        private string _accessToken;
        private string _userId;

        // UI element references
        private TextBox _serverUrlBox;
        private TextBox _usernameBox;
        private PasswordBox _passwordBox;
        private TextBlock _statusBlock;
        private StackPanel _loginPanel;
        private StackPanel _mediaBrowserPanel;
        private GridView _libraryGridView;
        private GridView _mediaGridView;

        public MainPage()
        {
            this.InitializeComponent();
            CreateUI();
        }

        private void CreateUI()
        {
            // Create main grid
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Create login panel
            var loginPanel = new StackPanel
            {
                Name = "LoginPanel",
                Orientation = Orientation.Vertical,
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Server URL input
            var serverUrlLabel = new TextBlock { Text = "Server URL:", Margin = new Thickness(0, 0, 0, 5) };
            var serverUrlBox = new TextBox
            {
                Name = "ServerUrlBox",
                PlaceholderText = "https://your-jellyfin-server.com",
                Margin = new Thickness(0, 0, 0, 10),
                Width = 300
            };

            // Username input
            var usernameLabel = new TextBlock { Text = "Username:", Margin = new Thickness(0, 0, 0, 5) };
            var usernameBox = new TextBox
            {
                Name = "UsernameBox",
                PlaceholderText = "Username",
                Margin = new Thickness(0, 0, 0, 10),
                Width = 300
            };

            // Password input
            var passwordLabel = new TextBlock { Text = "Password:", Margin = new Thickness(0, 0, 0, 5) };
            var passwordBox = new PasswordBox
            {
                Name = "PasswordBox",
                PlaceholderText = "Password",
                Margin = new Thickness(0, 0, 0, 10),
                Width = 300
            };

            // Login button
            var loginButton = new Button
            {
                Name = "LoginButton",
                Content = "Login",
                Margin = new Thickness(0, 0, 0, 10),
                Width = 300
            };
            loginButton.Click += LoginButton_Click;

            // Status text
            var statusBlock = new TextBlock
            {
                Name = "StatusBlock",
                Text = "",
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Add login controls to panel
            loginPanel.Children.Add(serverUrlLabel);
            loginPanel.Children.Add(serverUrlBox);
            loginPanel.Children.Add(usernameLabel);
            loginPanel.Children.Add(usernameBox);
            loginPanel.Children.Add(passwordLabel);
            loginPanel.Children.Add(passwordBox);
            loginPanel.Children.Add(loginButton);
            loginPanel.Children.Add(statusBlock);

            // Create media browser panel (initially hidden)
            var mediaBrowserPanel = new StackPanel
            {
                Name = "MediaBrowserPanel",
                Orientation = Orientation.Vertical,
                Margin = new Thickness(20),
                Visibility = Visibility.Collapsed
            };

            // Media browser title
            var mediaBrowserTitle = new TextBlock
            {
                Text = "Media Libraries",
                FontSize = 24,
                FontWeight = Windows.UI.Text.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Library GridView
            var libraryGridView = new GridView
            {
                Name = "LibraryGridView",
                Margin = new Thickness(0, 0, 0, 20)
            };
            libraryGridView.ItemClick += MediaGridView_ItemClick;
            libraryGridView.IsItemClickEnabled = true;

            // Create data template for library items programmatically
            var libraryItemTemplate = CreateLibraryItemTemplate();
            libraryGridView.ItemTemplate = libraryItemTemplate;

            // Media GridView (for individual media items)
            var mediaGridView = new GridView
            {
                Name = "MediaGridView",
                Visibility = Visibility.Collapsed
            };
            mediaGridView.ItemClick += MediaGridView_ItemClick;
            mediaGridView.IsItemClickEnabled = true;

            // Add media browser controls to panel
            mediaBrowserPanel.Children.Add(mediaBrowserTitle);
            mediaBrowserPanel.Children.Add(libraryGridView);
            mediaBrowserPanel.Children.Add(mediaGridView);

            // Add panels to main grid
            Grid.SetRow(loginPanel, 0);
            Grid.SetRow(mediaBrowserPanel, 1);
            mainGrid.Children.Add(loginPanel);
            mainGrid.Children.Add(mediaBrowserPanel);

            // Set main grid as page content
            this.Content = mainGrid;

            // Store references to UI elements for later use
            _serverUrlBox = serverUrlBox;
            _usernameBox = usernameBox;
            _passwordBox = passwordBox;
            _statusBlock = statusBlock;
            _loginPanel = loginPanel;
            _mediaBrowserPanel = mediaBrowserPanel;
            _libraryGridView = libraryGridView;
            _mediaGridView = mediaGridView;
        }

        private DataTemplate CreateLibraryItemTemplate()
        {
            var template = new DataTemplate();

            // Create the template using XAML string
            var xamlTemplate = @"
                <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                    <Border Width='300' Height='200' Margin='10' BorderBrush='Gray' BorderThickness='1' Background='DarkGray'>
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height='*'/>
                                <RowDefinition Height='Auto'/>
                            </Grid.RowDefinitions>
                            <Image Grid.Row='0' Stretch='UniformToFill' HorizontalAlignment='Center' VerticalAlignment='Center' Source='{Binding ImageUrl}'/>
                            <Border Grid.Row='1' HorizontalAlignment='Center' VerticalAlignment='Center' Background='#80000000' Padding='8' Margin='5'>
                                <TextBlock FontSize='16' FontWeight='Bold' TextWrapping='Wrap' HorizontalAlignment='Center' VerticalAlignment='Center' Foreground='White' Text='{Binding Name}'/>
                            </Border>
                        </Grid>
                    </Border>
                </DataTemplate>";

            template = (DataTemplate)Windows.UI.Xaml.Markup.XamlReader.Load(xamlTemplate);

            return template;
        }

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
                var loginResult = await LoginToJellyfin(serverUrl, username, password);
                if (loginResult.Success)
                {
                    _statusBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
                    _statusBlock.Text = $"Login successful! UserId: {loginResult.UserId}";

                    // Store login credentials for API calls
                    _serverUrl = serverUrl;
                    _accessToken = loginResult.Token;
                    _userId = loginResult.UserId;

                    // Navigate to media browser UI
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

        private async Task<JellyfinLoginResult> LoginToJellyfin(string serverUrl, string username, string password)
        {
            using (var client = new HttpClient())
            {
                // Set required headers for Jellyfin API
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    "MediaBrowser Client=\"JellyfinWM\", Device=\"Windows 10 Mobile\", DeviceId=\"unique-device-id\", Version=\"1.0\"");

                var loginInfo = new
                {
                    Username = username,
                    Pw = password // Changed from "Password" to "Pw" for Jellyfin compatibility
                };
                var json = JsonConvert.SerializeObject(loginInfo);
                Console.WriteLine(json); // Or Debug.WriteLine(json) JsonConvert.SerializeObject(loginInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{serverUrl}/Users/AuthenticateByName";

                try
                {
                    var response = await client.PostAsync(url, content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Show full request for debugging
                        return new JellyfinLoginResult { Success = false, Error = $"Status: {(int)response.StatusCode} {response.StatusCode}\nSent: {json}\nBody: {result}" };
                    }

                    dynamic jsonResp = JsonConvert.DeserializeObject(result);
                    string token = jsonResp.AccessToken;
                    string userId = jsonResp.User.Id;
                    return new JellyfinLoginResult { Success = true, UserId = userId, Token = token };
                }
                catch (Exception ex)
                {
                    return new JellyfinLoginResult { Success = false, Error = $"Exception: {ex.Message}\nStack: {ex.StackTrace}" };
                }
            }
        }

        private async Task NavigateToMediaBrowser()
        {
            try
            {
                // Hide login UI
                _loginPanel.Visibility = Visibility.Collapsed;

                // Show media browser UI
                _mediaBrowserPanel.Visibility = Visibility.Visible;

                // Load and display libraries
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
                using (var client = new HttpClient())
                {
                    // Set headers for authenticated API call
                    client.DefaultRequestHeaders.Add("User-Agent", "JellyfinMobile/1.0");
                    client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                        $"MediaBrowser Client=\"JellyfinMobile\", Device=\"Windows10Mobile\", DeviceId=\"unique-device-id\", Version=\"1.0\", Token=\"{_accessToken}\"");

                    // Get user's media libraries
                    var librariesUrl = $"{_serverUrl}/Users/{_userId}/Views";
                    var response = await client.GetAsync(librariesUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        _statusBlock.Text = $"API Response: {result}"; // Debug info

                        dynamic libraryData = JsonConvert.DeserializeObject(result);

                        var libraries = new List<MediaItem>();

                        if (libraryData.Items != null)
                        {
                            foreach (var library in libraryData.Items)
                            {
                                string libraryId = library.Id?.ToString();
                                string libraryName = library.Name?.ToString();
                                string imageTag = library.ImageTags?.Primary?.ToString();

                                var mediaItem = new MediaItem
                                {
                                    Id = libraryId ?? "unknown",
                                    Name = libraryName ?? "Unknown Library",
                                    Type = library.Type?.ToString() ?? "Library",
                                    Overview = library.Overview?.ToString() ?? "",
                                    ImageUrl = GetImageUrl(libraryId, imageTag)
                                };

                                libraries.Add(mediaItem);
                            }
                        }

                        _statusBlock.Text = $"Found {libraries.Count} libraries";

                        // Populate the GridView with library items
                        _libraryGridView.ItemsSource = libraries;
                    }
                    else
                    {
                        _statusBlock.Text = $"Failed to fetch libraries: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                _statusBlock.Text = $"Error fetching libraries: {ex.Message}";
            }
        }

        private async Task LoadMediaIntoUI()
        {
            if (string.IsNullOrEmpty(_serverUrl) || string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_userId))
            {
                return;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    // Set headers for authenticated API call
                    client.DefaultRequestHeaders.Add("User-Agent", "JellyfinMobile/1.0");
                    client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                        $"MediaBrowser Client=\"JellyfinMobile\", Device=\"Windows10Mobile\", DeviceId=\"unique-device-id\", Version=\"1.0\", Token=\"{_accessToken}\"");

                    // Get user's media libraries
                    var librariesUrl = $"{_serverUrl}/Users/{_userId}/Items?Recursive=true&IncludeItemTypes=Movie,Series,Episode,Audio";
                    var response = await client.GetAsync(librariesUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        dynamic mediaData = JsonConvert.DeserializeObject(result);

                        var mediaItems = new List<MediaItem>();
                        foreach (var item in mediaData.Items)
                        {
                            string itemId = item.Id?.ToString();
                            string imageTag = item.ImageTags?.Primary?.ToString();

                            mediaItems.Add(new MediaItem
                            {
                                Id = itemId,
                                Name = item.Name,
                                Type = item.Type,
                                Overview = item.Overview,
                                ImageUrl = GetImageUrl(itemId, imageTag)
                            });
                        }

                        // Populate the GridView with media items
                        _mediaGridView.ItemsSource = mediaItems;
                    }
                    else
                    {
                        _statusBlock.Text = $"Failed to fetch media: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                _statusBlock.Text = $"Error fetching media: {ex.Message}";
            }
        }

        private string GetImageUrl(string itemId, string imageTag)
        {
            if (string.IsNullOrEmpty(imageTag) || string.IsNullOrEmpty(itemId))
                return null;

            return $"{_serverUrl}/Items/{itemId}/Images/Primary?tag={imageTag}&width=300&height=450";
        }

        private void MediaGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var mediaItem = e.ClickedItem as MediaItem;
            if (mediaItem != null)
            {
                // Handle media item selection - could navigate to details page or start playback
                _statusBlock.Text = $"Selected: {mediaItem.Name}";
            }
        }
    }

    public class MediaItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Overview { get; set; }
        public string ImageUrl { get; set; }
    }

    public class JellyfinLoginResult
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }
    }
}
