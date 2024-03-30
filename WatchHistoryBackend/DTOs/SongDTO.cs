namespace WatchHistoryBackend.DTOs
{
    public class SongDTO
    {
        public string Name { get; set; } = null!;
        public string ArtistName { get; set; } = null!;
        public int NumberOfListens { get; set; }
    }
}
