using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using WatchHistoryBackend.Data;
using WatchHistoryBackend.DTOs;
using WatchHistoryBackend.Models;
using WatchHistoryBackend.Services.Mapping;
using WatchHistoryBackend.Services.Read;

namespace WatchHistoryBackend.Controllers
{
    [ApiController]
    [Route("api/song/")]
    public partial class SongController(MusicContext context,
                                        IReadService<Song> readService,
                                        IMapper<Song, SongDTO> mapper) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? q, [FromQuery] bool? strict, [FromQuery] int? offset, [FromQuery] int? limit)
        {
            var result = await readService.Get(q, strict, offset, limit);
            return Ok(result.Select(mapper.Map));
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] SongListDTO songList)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            foreach (var songListenList in songList.Songs)
            {
                SongListDTO.SongDTO songDTO = songListenList.First().Song;
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
        private static CleanSongDTO CleanUp(string songName, string artistName)
        {
            string newSongName = string.Join(' ', songName.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            string? newArtistName = string.Join(' ', artistName.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

            newSongName = BracketsRegex().Replace(newSongName, string.Empty);
            newArtistName = BracketsRegex().Replace(newArtistName, string.Empty);

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

        [GeneratedRegex(@"\([^()]*\)|\[[^[]*\]")]
        private static partial Regex BracketsRegex();
    }
}
