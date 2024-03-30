namespace WatchHistoryBackend.Services.Mapping
{
    public interface IMapper<Entity, DTO> where Entity : class where DTO : class
    {
        DTO Map(Entity entity);
    }
}
