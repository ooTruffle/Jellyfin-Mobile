namespace JellyfinMobile.Models
{
    public class MediaItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "Movie", "Series", etc.
        public string ImageUrl { get; set; }
        public string Overview { get; set; }
        public string ParentId { get; set; }
    }
}