namespace WatchHistoryBackend.Models
{
    public class ListenedSong
    {
        public int Id { get; set; }
        public string Time { get; set; } = null!;
        public Song Song { get; set; } = null!;
        public int SongId { get; set;}
    }
}
