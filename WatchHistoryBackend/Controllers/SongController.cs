using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchHistoryBackend.Data;
using WatchHistoryBackend.Models;

namespace WatchHistoryBackend.Controllers
{
    [ApiController]
    [Route("api/song/")]
    public class SongController(MusicContext context) : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var songs = context.Songs.Include(x => x.Listens);

            var songDTOs = songs.Select(x => new {
                name = x.Name,
                artist = x.Artist,
                numberOfListens = x.Listens.Count()
            });

            return Ok(songDTOs);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] SongList songList)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            foreach (var songListenList in songList.Songs)
            {
                SongList.SongDTO songDTO = songListenList.First().Song;
                Song song = new()
                {
                    Name = songDTO.Name ?? "---> Empty / 404",
                    SongLink = songDTO.SongLink ?? "---> Empty / 404",
                    Artist = songDTO.Artist ?? "---> Empty / 404",
                    ArtistLink = songDTO.ArtistLink ?? "---> Empty / 404",
                };

                await context.Songs.AddAsync(song);
                await context.SaveChangesAsync();

                IEnumerable<ListenedSong> listens = songListenList.Select(x => new ListenedSong()
                {
                    SongId = song.Id,
                    Time = x.Time
                });

                await context.ListenedSongs.AddRangeAsync(listens);
            }

            await context.SaveChangesAsync();
            return Ok();
        }

        public class SongList
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
}
