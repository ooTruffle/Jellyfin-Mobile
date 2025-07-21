namespace JellyfinMobile.Models
{
    public class JellyfinLoginResult
    {
        public bool Success { get; set; }
        public string UserId { get; set; }
        public string Token { get; set; }
        public string Error { get; set; }
    }
}