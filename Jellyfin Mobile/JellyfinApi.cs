using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class JellyfinApi
{
    private HttpClient _client = new HttpClient();
    public string AccessToken { get; private set; }
    public string UserId { get; private set; }
    public string ServerUrl { get; private set; }

    public async Task<bool> LoginAsync(string serverUrl, string username, string password)
    {
        ServerUrl = serverUrl.TrimEnd('/');
        var loginInfo = new { Username = username, Password = password };
        var content = new StringContent(JsonConvert.SerializeObject(loginInfo), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{ServerUrl}/Users/AuthenticateByName", content);
        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadAsStringAsync();
        dynamic json = JsonConvert.DeserializeObject(result);
        AccessToken = json.AccessToken;
        UserId = json.User.Id;
        return true;
    }
}