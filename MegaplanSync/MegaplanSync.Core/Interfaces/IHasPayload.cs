namespace MegaplanSync.Core.Interfaces;

public interface IHasPayload
{
    abstract static string GetPayload(long afterId = Consts.ID_BEFORE_START_ID, int limit = Consts.JSON_ENTRIES_LIMIT, string filterId = null);
}
