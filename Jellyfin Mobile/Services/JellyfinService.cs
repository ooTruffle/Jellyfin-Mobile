using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using JellyfinMobile.Models;

namespace JellyfinMobile.Services
{
    public class JellyfinService
    {
        private static string GetDeviceString()
        {
            string osString = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            if (osString.IndexOf("Mobile", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Windows 10 Mobile";
            if (osString.IndexOf("Windows 10", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Windows 10";
            return osString.Trim();
        }

        public async Task<JellyfinLoginResult> LoginAsync(string serverUrl, string username, string password)
        {
            using (var client = new HttpClient())
            {
                string deviceString = GetDeviceString();
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    $"MediaBrowser Client=\"JellyfinWM\", Device=\"{deviceString}\", DeviceId=\"unique-device-id\", Version=\"1.0\"");

                var loginInfo = new { Username = username, Pw = password };
                var json = JsonConvert.SerializeObject(loginInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{serverUrl}/Users/AuthenticateByName";

                try
                {
                    var response = await client.PostAsync(url, content);
                    var result = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        return new JellyfinLoginResult { Success = false, Error = $"Status: {(int)response.StatusCode} {response.StatusCode}\nSent: {json}\nBody: {result}" };
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
                string deviceString = GetDeviceString();
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    $"MediaBrowser Client=\"JellyfinWM\", Device=\"{deviceString}\", DeviceId=\"unique-device-id\", Version=\"1.0\", Token=\"{accessToken}\"");

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
                            libraries.Add(new MediaItem
                            {
                                Id = library.Id?.ToString(),
                                Name = library.Name?.ToString(),
                                Type = library.CollectionType?.ToString() ?? library.Type?.ToString(),
                                Overview = library.Overview?.ToString() ?? "",
                                ImageUrl = GetMediaImageUrl(serverUrl, library.Id?.ToString(), library.ImageTags, null)
                            });
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
                string deviceString = GetDeviceString();
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    $"MediaBrowser Client=\"JellyfinWM\", Device=\"{deviceString}\", DeviceId=\"unique-device-id\", Version=\"1.0\", Token=\"{accessToken}\"");

                var url = $"{serverUrl}/Users/{userId}/Items?ParentId={libraryId}&Recursive=false";
                var response = await client.GetAsync(url);
                var mediaItems = new List<MediaItem>();
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    dynamic mediaData = JsonConvert.DeserializeObject(result);
                    if (mediaData.Items != null)
                    {
                        foreach (var item in mediaData.Items)
                        {
                            string type = item.Type?.ToString();
                            if (type == "Series" || type == "Movie" || type == "MusicAlbum")
                            {
                                mediaItems.Add(new MediaItem
                                {
                                    Id = item.Id?.ToString(),
                                    Name = item.Name?.ToString(),
                                    Type = type,
                                    Overview = item.Overview?.ToString() ?? "",
                                    ImageUrl = GetMediaImageUrl(serverUrl, item.Id?.ToString(), item.ImageTags, item.SeriesId?.ToString())
                                });
                            }
                        }
                    }
                }
                return mediaItems;
            }
        }

        public async Task<List<MediaItem>> GetSeasonsAsync(string serverUrl, string userId, string accessToken, string showId)
        {
            using (var client = new HttpClient())
            {
                string deviceString = GetDeviceString();
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    $"MediaBrowser Client=\"JellyfinWM\", Device=\"{deviceString}\", DeviceId=\"unique-device-id\", Version=\"1.0\", Token=\"{accessToken}\"");
                var url = $"{serverUrl}/Shows/{showId}/Seasons?UserId={userId}";
                var response = await client.GetAsync(url);
                var seasons = new List<MediaItem>();
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    dynamic seasonData = JsonConvert.DeserializeObject(result);
                    if (seasonData.Items != null)
                    {
                        foreach (var season in seasonData.Items)
                        {
                            seasons.Add(new MediaItem
                            {
                                Id = season.Id?.ToString(),
                                Name = season.Name?.ToString(),
                                Type = "Season",
                                Overview = season.Overview?.ToString() ?? "",
                                ImageUrl = GetMediaImageUrl(serverUrl, season.Id?.ToString(), season.ImageTags, showId)
                            });
                        }
                    }
                }
                return seasons;
            }
        }

        public async Task<List<MediaItem>> GetEpisodesAsync(string serverUrl, string userId, string accessToken, string seasonId)
        {
            using (var client = new HttpClient())
            {
                string deviceString = GetDeviceString();
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    $"MediaBrowser Client=\"JellyfinWM\", Device=\"{deviceString}\", DeviceId=\"unique-device-id\", Version=\"1.0\", Token=\"{accessToken}\"");

                var url = $"{serverUrl}/Users/{userId}/Items?ParentId={seasonId}&IncludeItemTypes=Episode";
                var response = await client.GetAsync(url);
                var episodes = new List<MediaItem>();
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    dynamic episodeData = JsonConvert.DeserializeObject(result);
                    if (episodeData.Items != null)
                    {
                        foreach (var episode in episodeData.Items)
                        {
                            episodes.Add(new MediaItem
                            {
                                Id = episode.Id?.ToString(),
                                Name = episode.Name?.ToString(),
                                Type = "Episode",
                                Overview = episode.Overview?.ToString() ?? "",
                                ImageUrl = GetMediaImageUrl(serverUrl, episode.Id?.ToString(), episode.ImageTags, episode.SeriesId?.ToString())
                            });
                        }
                    }
                }
                return episodes;
            }
        }

        // Returns both the URL and the raw JSON from Jellyfin
        // THIS IS THE CHANGED METHOD THAT FORCES TRANSCODING TO H.264/MP4
        public async Task<(string Url, string RawJson)> GetPlayableUrlAndRawAsync(string serverUrl, string accessToken, string itemId, string userId)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Emby-Token", accessToken);
                // Force transcoding to H.264/MP4 and add MaxStreamingBitrate to help force transcoding
                var url = $"{serverUrl}/Items/{itemId}/PlaybackInfo?UserId={userId}" +
                    $"&MediaSourceId={itemId}" +
                    $"&VideoCodec=h264" +
                    $"&AudioCodec=aac" +
                    $"&Container=mp4" +
                    $"&TranscodingContainer=mp4" +
                    $"&MaxStreamingBitrate=2000000";

                var response = await client.GetAsync(url);
                var result = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(result);

                string playUrl = null;
                if (data.MediaSources != null && data.MediaSources.Count > 0)
                {
                    var ms = data.MediaSources[0];
                    // Only use TranscodingUrl if present and starts with "/"
                    if (ms.TranscodingUrl != null && ms.TranscodingUrl.ToString().StartsWith("/"))
                        playUrl = $"{serverUrl}{ms.TranscodingUrl}?api_key={accessToken}";
                    else if (ms.TranscodingUrl != null &&
                        (ms.TranscodingUrl.ToString().StartsWith("http://") || ms.TranscodingUrl.ToString().StartsWith("https://")))
                        playUrl = ms.TranscodingUrl.ToString();
                    // Do NOT use ms.Path if Protocol == "File"
                }
                return (playUrl, result);
            }
        }

        private string GetMediaImageUrl(string serverUrl, string itemId, dynamic imageTags, string seriesId = null)
        {
            string tag = null;
            string type = null;
            if (imageTags != null && imageTags.Primary != null)
            {
                tag = imageTags.Primary.ToString();
                type = "Primary";
            }
            else if (imageTags != null && imageTags.Thumb != null)
            {
                tag = imageTags.Thumb.ToString();
                type = "Thumb";
            }
            else if (imageTags != null && imageTags.Backdrop != null)
            {
                tag = imageTags.Backdrop.ToString();
                type = "Backdrop";
            }

            if (!string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(type))
                return $"{serverUrl}/Items/{itemId}/Images/{type}?tag={tag}&width=200&height=300";

            // Fallback to series poster if available
            if (!string.IsNullOrEmpty(seriesId))
                return $"{serverUrl}/Items/{seriesId}/Images/Primary?width=200&height=300";

            // Fallback to a generic local asset
            return "ms-appx:///Assets/placeholder.png";
        }

        /// <summary>
        /// Gets the latest episode details from Jellyfin, including Overview.
        /// </summary>
        public async Task<MediaItem> GetEpisodeDetailsAsync(string serverUrl, string userId, string accessToken, string episodeId)
        {
            using (var client = new HttpClient())
            {
                string deviceString = GetDeviceString();
                client.DefaultRequestHeaders.Add("User-Agent", "JellyfinWM/1.0");
                client.DefaultRequestHeaders.Add("X-Emby-Authorization",
                    $"MediaBrowser Client=\"JellyfinWM\", Device=\"{deviceString}\", DeviceId=\"unique-device-id\", Version=\"1.0\", Token=\"{accessToken}\"");

                var url = $"{serverUrl}/Users/{userId}/Items/{episodeId}";
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    dynamic item = JsonConvert.DeserializeObject(result);
                    return new MediaItem
                    {
                        Id = item.Id?.ToString(),
                        Name = item.Name?.ToString(),
                        Type = "Episode",
                        Overview = item.Overview?.ToString() ?? "",
                        ImageUrl = GetMediaImageUrl(serverUrl, item.Id?.ToString(), item.ImageTags, item.SeriesId?.ToString())
                    };
                }
                return null;
            }
        }
    }
}