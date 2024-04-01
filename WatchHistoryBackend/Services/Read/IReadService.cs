namespace WatchHistoryBackend.Services.Read
{
    public interface IReadService<Entity> where Entity : class
    {
        Task<Entity> Get(string id);
        Task<IEnumerable<Entity>> Get(string? q, bool? strict, int? offset, int? limit);
    }
}
