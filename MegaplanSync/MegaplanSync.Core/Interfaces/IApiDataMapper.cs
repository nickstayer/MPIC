using MegaplanSync.Core.Models.Contractor;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.DealStatusHistory;
using MegaplanSync.Core.Models.Department;
using MegaplanSync.Core.Models.Employee;

namespace MegaplanSync.Core.Interfaces;

public interface IApiDataMapper
{
    T MapEntity<T>(string json) where T : class?;
    List<T> MapEntities<T>(string json);
}
