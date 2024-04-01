using WatchHistoryBackend.DTOs;
using WatchHistoryBackend.Models;

namespace WatchHistoryBackend.Services.Mapping
{
    public class SongMapper : IMapper<Song, SongDTO>
    {
        public SongDTO Map(Song entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            ArtistName = entity.Artist,
            NumberOfListens = entity.Listens.Count()
        };
    }
}
