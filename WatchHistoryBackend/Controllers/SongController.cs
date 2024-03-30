using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchHistoryBackend.Data;
using WatchHistoryBackend.DTOs;
using WatchHistoryBackend.Models;
using WatchHistoryBackend.Services.Mapping;

namespace WatchHistoryBackend.Controllers
{
    [ApiController]
    [Route("api/song/")]
    public class SongController(MusicContext context, IMapper<Song, SongDTO> mapper) : ControllerBase
    {
        [HttpGet]
        public IActionResult Get([FromQuery] string? artist, [FromQuery] string? song)
        {
            var songs = context.Songs.Include(x => x.Listens);

            if (artist is null && song is null)
                return Ok(songs.Select(mapper.Map));

            IEnumerable<Song> result = [];
            if (artist is not null)
                result = songs.Where(x => x.Artist.ToLower().Contains(artist.ToLower()));

            if(song is not null)
                result = songs.Where(x => x.Name.ToLower().Contains(song.ToLower()));

            return Ok(result.Select(mapper.Map));
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

        [HttpPut("cleanup")]
        public async Task<IActionResult> CleanUp()
        {
            foreach (var song in context.Songs)
            {
                song.Name = string.Join(' ', song.Name.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                song.SongLink = string.Join(' ', song.SongLink.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                song.Artist = string.Join(' ', song.Artist.Replace("- Topic", "").Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                song.ArtistLink = string.Join(' ', song.ArtistLink.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
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
