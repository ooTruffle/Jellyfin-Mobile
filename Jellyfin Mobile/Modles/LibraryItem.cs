namespace JellyfinMobile.Models
{
    public class LibraryItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "movie", "series", etc.
        public string ImageUrl { get; set; }
    }
}