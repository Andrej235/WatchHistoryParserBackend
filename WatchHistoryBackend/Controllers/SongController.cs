using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
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
        public IActionResult Get([FromQuery] string? q, [FromQuery] bool? strict)
        {
            var songs = context.Songs.Include(x => x.Listens);

            IEnumerable<Song> result = songs;
            var criterias = DecipherQueryString(q);

            if (strict ?? false)
                foreach (var x in criterias)
                    result = result.Where(x);
            else
                result = ApplyNonStrictCriterias(result, criterias);

            return Ok(result.Select(mapper.Map));
        }

        private static IEnumerable<Func<Song, bool>> DecipherQueryString(string? q)
        {
            if (q is null) return [];

            //artist=againstthecurrent;song=blindfolded;fas

            var keyValuePairs = q.Split(';').Select(x => x.Split('=').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))).Where(x => x.Count() == 2);
            return keyValuePairs.Select(x => DecipherQueryString(x.First(), x.Last()));
        }

        private static Func<Song, bool> DecipherQueryString(string key, string value)
        {
            return key switch
            {
                "song" => x => x.Name.Trim(' ').ToLower().Contains(value.Trim(' ').ToLower()),
                "name" => x => x.Name.Trim(' ').ToLower().Contains(value.Trim(' ').ToLower()),
                "artist" => x => x.Artist.Trim(' ').ToLower().Contains(value.Trim(' ').ToLower()),
                _ => _ => true
            };
        }

        protected virtual IEnumerable<T> ApplyNonStrictCriterias<T>(IEnumerable<T> entitiesQueryable, IEnumerable<Func<T, bool>> criterias)
        {
            if (!criterias.Any())
                return entitiesQueryable;

            return criterias
            .Select(x => entitiesQueryable.Where(x))
            .SelectMany(x => x)
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .Select(x => x.First())
            .AsQueryable();
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
