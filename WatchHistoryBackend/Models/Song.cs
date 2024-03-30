namespace WatchHistoryBackend.Models
{
    public class Song
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string SongLink { get; set; } = null!;
        public string Artist { get; set; } = null!;
        public string ArtistLink { get; set; } = null!;
        public IEnumerable<ListenedSong> Listens { get; set; } = null!;
    }
}
