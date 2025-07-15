using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JellyfinMobile.Services
{
    public class JellyfinService
    {
        public async Task<JellyfinLoginResult> LoginAsync(string serverUrl, string username, string password)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    "MediaBrowser Client=\"JellyfinWM\", Device=\"Windows 10 Mobile\", DeviceId=\"unique-device-id\", Version=\"1.0\"");

                var loginInfo = new { Username = username, Pw = password };
                var json = JsonConvert.SerializeObject(loginInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{serverUrl}/Users/AuthenticateByName";

                try
                {
                    var response = await client.PostAsync(url, content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
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

        public async Task<List<MediaItem>> GetLibrariesAsync(string serverUrl, string userId, string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    $"MediaBrowser Client=\"JellyfinWM\", Device=\"Windows 10 Mobile\", DeviceId=\"unique-device-id\", Version=\"1.0\", Token=\"{accessToken}\"");

                var librariesUrl = $"{serverUrl}/Users/{userId}/Views";
                var response = await client.GetAsync(librariesUrl);

                var libraries = new List<MediaItem>();
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    dynamic libraryData = JsonConvert.DeserializeObject(result);

                    if (libraryData.Items != null)
                    {
                        foreach (var library in libraryData.Items)
                        {
                            string libraryId = library.Id?.ToString();
                            string libraryName = library.Name?.ToString();
                            string libraryType = library.Type?.ToString();
                            string imageTag = library.ImageTags?.Primary?.ToString();

                            if (libraryType == "livetv" || libraryName?.ToLower().Contains("live tv") == true)
                                continue;

                            var mediaItem = new MediaItem
                            {
                                Id = libraryId ?? "unknown",
                                Name = libraryName ?? "Unknown Library",
                                Type = libraryType ?? "Library",
                                Overview = library.Overview?.ToString() ?? "",
                                ImageUrl = GetImageUrl(serverUrl, libraryId, imageTag)
                            };

                            libraries.Add(mediaItem);
                        }
                    }
                }

                return libraries;
            }
        }

        public async Task<List<MediaItem>> GetLibraryContentAsync(string serverUrl, string userId, string accessToken, string libraryId)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    $"MediaBrowser Client=\"JellyfinWM\", Device=\"Windows 10 Mobile\", DeviceId=\"unique-device-id\", Version=\"1.0\", Token=\"{accessToken}\"");

                var libraryContentUrl = $"{serverUrl}/Users/{userId}/Items?ParentId={libraryId}&IncludeItemTypes=Movie,Series,Episode,Audio";
                var response = await client.GetAsync(libraryContentUrl);

                var mediaItems = new List<MediaItem>();
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    dynamic mediaData = JsonConvert.DeserializeObject(result);

                    if (mediaData.Items != null)
                    {
                        foreach (var item in mediaData.Items)
                        {
                            string itemId = item.Id?.ToString();
                            string itemName = item.Name?.ToString();
                            string itemType = item.Type?.ToString();
                            string imageTag = item.ImageTags?.Primary?.ToString();

                            var mediaItem = new MediaItem
                            {
                                Id = itemId ?? "unknown",
                                Name = itemName ?? "Unknown Item",
                                Type = itemType ?? "Unknown",
                                Overview = item.Overview?.ToString() ?? "",
                                ImageUrl = GetMediaImageUrl(serverUrl, itemId, imageTag)
                            };

                            mediaItems.Add(mediaItem);
                        }
                    }
                }

                return mediaItems;
            }
        }

        private string GetImageUrl(string serverUrl, string itemId, string imageTag)
        {
            if (string.IsNullOrEmpty(imageTag) || string.IsNullOrEmpty(itemId))
                return null;

            return $"{serverUrl}/Items/{itemId}/Images/Primary?tag={imageTag}&width=160&height=100";
        }

        private string GetMediaImageUrl(string serverUrl, string itemId, string imageTag)
        {
            if (string.IsNullOrEmpty(imageTag) || string.IsNullOrEmpty(itemId))
                return null;

            return $"{serverUrl}/Items/{itemId}/Images/Primary?tag={imageTag}&width=120&height=180";
        }
    }
}