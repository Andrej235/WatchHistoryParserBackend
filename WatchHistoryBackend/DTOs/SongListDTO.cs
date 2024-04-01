namespace WatchHistoryBackend.DTOs
{
    public class SongListDTO
    {
        public IEnumerable<IEnumerable<ListenedSongDTO>> Songs { get; set; } = null!;

        public class ListenedSongDTO
        {
            public SongDTO Song { get; set; } = null!;
            public string Time { get; set; } = null!;
        }

        public class SongDTO
        {
            public string? Name { get; set; }
            public string? SongLink { get; set; }
            public string? Artist { get; set; }
            public string? ArtistLink { get; set; }
        }
    }
}
