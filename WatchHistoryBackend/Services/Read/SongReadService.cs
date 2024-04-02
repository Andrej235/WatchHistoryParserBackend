using Microsoft.EntityFrameworkCore;
using WatchHistoryBackend.Data;
using WatchHistoryBackend.Exceptions;
using WatchHistoryBackend.Models;
using WatchHistoryBackend.Utils;

namespace WatchHistoryBackend.Services.Read
{
    public class SongReadService(MusicContext context) : IReadService<Song>
    {
        public async Task<Song> Get(string id) => await context.Songs.FindAsync(id) ?? throw new EntityNotFoundException($"Song with id {id} not found");

        public Task<IEnumerable<Song>> Get(string? q, bool? strict, int? offset, int? limit)
        {
            return Task.Run(() =>
            {
                var songs = context.Songs.Include(x => x.Listens);

                var result = string.IsNullOrWhiteSpace(q) ? songs : ApplyCriterias(songs, DecipherQueryString(q), strict ?? false);
                return result.ApplySkipAndOffset(offset, limit);
            });
        }

        private static IEnumerable<Func<Song, bool>> DecipherQueryString(string q)
        {
            var split = q.Split('&');
            if (split.Length == 0)
                return [];
            else if (split.Length == 1 && !split[0].Contains('='))
                return [x => x.Name.Trim(' ').ToLower().Contains(split[0].Trim(' ').ToLower()) || x.Artist.Trim(' ').ToLower().Contains(split[0].Trim(' ').ToLower())];

            var keyValuePairs = split.Select(x => x.Split('=').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))).Where(x => x.Count() == 2);
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

        private static IEnumerable<T> ApplyCriterias<T>(IEnumerable<T> entitiesQueryable, IEnumerable<Func<T, bool>> criterias, bool strict) => strict
            ? ApplyStrictCriterias(entitiesQueryable, criterias)
            : ApplyNonStrictCriterias(entitiesQueryable, criterias);

        private static IEnumerable<T> ApplyStrictCriterias<T>(IEnumerable<T> entitiesQueryable, IEnumerable<Func<T, bool>> criterias)
        {
            foreach (var x in criterias)
                entitiesQueryable = entitiesQueryable.Where(x);

            return entitiesQueryable;
        }

        private static IEnumerable<T> ApplyNonStrictCriterias<T>(IEnumerable<T> entitiesQueryable, IEnumerable<Func<T, bool>> criterias)
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
    }
}
