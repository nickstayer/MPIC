using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models;
using System.Text.Json;

namespace MegaplanSync.Core
{
    /// <summary>
    /// Маппер JSON-ответов API Мегаплана в модели.
    /// </summary>
    public class ApiDataMapper(ILogger logger) : IApiDataMapper
    {
        public List<T>? MapEntities<T>(string json)
        {
            var responseDeserialized = JsonSerializer.Deserialize<Responses<T>>(json);
            logger.LogDebug("Ответ десериализован");
            var entity = responseDeserialized?.Data;

            if (entity != null)
            {
                return entity;
            }

            return null;
        }
    }
}
