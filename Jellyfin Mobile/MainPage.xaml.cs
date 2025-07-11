using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;

namespace JellyfinMobile
{
    public sealed partial class MainPage : Page
    {
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
                var loginResult = await LoginToJellyfin(serverUrl, username, password);
                if (loginResult.Success)
                {
                    StatusBlock.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.Green);
                    StatusBlock.Text = $"Login successful! UserId: {loginResult.UserId}";
                    // TODO: Proceed to fetch media or move to next page
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

        private async Task<LoginResult> LoginToJellyfin(string serverUrl, string username, string password)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                var loginInfo = new
                {
                    Username = username,
                    Password = password,
                    App = "Jellyfin WM",
                    Device = "Windows Mobile"
                };
                var json = JsonConvert.SerializeObject(loginInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{serverUrl}/Users/AuthenticateByName";

                try
                {
                    var response = await client.PostAsync(url, content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Show full request for debugging
                        return new LoginResult { Success = false, Error = $"Status: {(int)response.StatusCode} {response.StatusCode}\nSent: {json}\nBody: {result}" };
                    }

                    dynamic jsonResp = JsonConvert.DeserializeObject(result);
                    string token = jsonResp.AccessToken;
                    string userId = jsonResp.User.Id;
                    return new LoginResult { Success = true, UserId = userId, Token = token };
                }
                catch (Exception ex)
                {
                    return new LoginResult { Success = false, Error = $"Exception: {ex.Message}\nStack: {ex.StackTrace}" };
                }
            }
        }
    }
}