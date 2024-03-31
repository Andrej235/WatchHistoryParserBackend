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
                var cleanSong = CleanUp(song.Name, song.Artist);
                song.Name = cleanSong.NewName;
                song.Artist = cleanSong.NewArtistName;
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

        public record CleanSongDTO(string NewName, string NewArtistName);
        private static CleanSongDTO CleanUp(string songName, string artistName)
        {
            string newSongName = string.Join(' ', songName.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            string? newArtistName = string.Join(' ', artistName.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

            newSongName = newSongName.Replace("(Official Music Video)", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("(Official Video)", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("(Visualizer)", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("(Official Lyric Video)", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("(Lyric Video)", "", StringComparison.OrdinalIgnoreCase)
                                     .Trim();

            newArtistName = newArtistName.Replace("Official", "", StringComparison.OrdinalIgnoreCase)
                                         .Replace("Topic", "", StringComparison.OrdinalIgnoreCase)
                                         .Replace("Band", "", StringComparison.OrdinalIgnoreCase)
                                         .Trim();

            if (!string.IsNullOrWhiteSpace(newArtistName) && newSongName != newArtistName)
                newSongName = newSongName.Replace(newArtistName, "");

            if (newArtistName.ToLower().Contains("record"))
            {
                //Deal with records / labels
                var separatedSongName = Separate(newSongName, "\"");
                if (string.IsNullOrWhiteSpace(separatedSongName))
                    newArtistName = newSongName.Split('-').FirstOrDefault();
                else
                {
                    newArtistName = newSongName.Replace(separatedSongName, "");
                    newSongName = separatedSongName;
                }
            }

            if (string.IsNullOrWhiteSpace(newArtistName))
                return new CleanSongDTO(newSongName, "Error 404");

            newSongName = newSongName.Replace(newArtistName, "");
            newSongName = newSongName.Replace("-", "").Replace("\"", "").Trim();
            newArtistName = newArtistName.Replace("-", "").Trim();

            //newSongName = string.Join(' ', newSongName.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            //newArtistName = string.Join(' ', newArtistName.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

            return new CleanSongDTO(newSongName, newArtistName);
        }

        private static string Separate(string name, string separator)
        {
            int start = name.IndexOf(separator) + 1;
            int end = name.IndexOf(separator, start);

            return start > 0 && end > start ? name[start..end] : string.Empty;
        }
    }
}
