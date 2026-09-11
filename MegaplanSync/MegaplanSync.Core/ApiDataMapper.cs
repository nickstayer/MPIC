using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models;
using MegaplanSync.Core.Models.Contractor;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.DealStatusHistory;
using MegaplanSync.Core.Models.Department;
using MegaplanSync.Core.Models.Employee;
using System.Text.Json;

namespace MegaplanSync.Core
{
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

        public T? MapEntity<T>(string json) where T : class?
        {
            var responseDeserialized = JsonSerializer.Deserialize<Response<T>>(json);
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
