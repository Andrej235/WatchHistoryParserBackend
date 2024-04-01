namespace WatchHistoryBackend.DTOs
{
    public class SongDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string ArtistName { get; set; } = null!;
        public int NumberOfListens { get; set; }
    }
}
