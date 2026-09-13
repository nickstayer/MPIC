using System.Text;

namespace MegaplanSync.Core.Models.Deal;

/// <summary>
/// JSON-нагрузка запроса сделок. Запрашиваются только поля, используемые MPIC.
/// </summary>
public class DealPayload
{
    public static string GetLastUpdatedDealsPayload(long afterId = Consts.ID_BEFORE_START_ID, int limit = Consts.JSON_ENTRIES_LIMIT)
    {
        var sb = new StringBuilder();
        sb.Append(@"{");
        sb.Append(fieldsBlock);
        sb.Append(@"""onlyRequestedFields"": true,");
        sb.Append(@"""sortBy"": [
            {
                ""contentType"": ""SortField"",
                ""fieldName"": ""timeUpdated"",
                ""desc"": true
            }
        ],");
        sb.Append($@"""limit"": {limit}");

        if (afterId != Consts.ID_BEFORE_START_ID)
        {
            sb.Append($@",
                ""pageAfter"": {{
                    ""id"": ""{afterId}"",
                    ""contentType"": ""Deal""
                }}");
        }

        sb.Append(@"}");
        return sb.ToString();
    }

    static string fieldsBlock = @"""fields"": [
                ""id"",
                ""number"",
                ""name"",
                ""contractor"",
                ""timeCreated"",
                ""timeUpdated""
            ],";
}
