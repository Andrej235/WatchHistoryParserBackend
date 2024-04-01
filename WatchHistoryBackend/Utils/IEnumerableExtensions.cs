namespace WatchHistoryBackend.Utils
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> ApplySkipAndOffset<T>(this IEnumerable<T> source, int? offset, int? limit)
        {
            offset ??= 0;
            limit ??= 10;

            return source.Skip(offset.Value).Take(limit.Value != -1 ? limit.Value : int.MaxValue);
        }
    }
}
