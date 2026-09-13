namespace MegaplanSync.Core.Interfaces;

public interface IApiDataMapper
{
    List<T>? MapEntities<T>(string json);
}
