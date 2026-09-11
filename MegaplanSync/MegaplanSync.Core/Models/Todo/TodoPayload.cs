using System.Text;

namespace MegaplanSync.Core.Models.Employee;

public class TodoPayload
{
    public static string GetPayload(long afterId = Consts.ID_BEFORE_START_ID, int limit = Consts.JSON_ENTRIES_LIMIT)
    {
        var sb = new StringBuilder();

        sb.Append("{");
        sb.Append(fieldsBlock);
        sb.Append(@$"""onlyRequestedFields"": true,
        ""limit"": {limit}");

        if (afterId != Consts.ID_BEFORE_START_ID)
        {
            sb.Append(@$",
            ""pageAfter"": {{
                ""id"": ""{afterId}"",
                ""contentType"": ""Todo""
            }}");
        }

        sb.Append(@"
    }");

        return sb.ToString();
    }

    public static string GetLastEntitiesPayload(int limit = Consts.JSON_ENTRIES_LIMIT)
    {
        var sb = new StringBuilder();
        sb.Append(@"{");
        sb.Append(fieldsBlock);
        sb.Append(@"""onlyRequestedFields"": true");
        sb.Append(@",""sortBy"": [
            {
                ""contentType"": ""SortField"",
                ""fieldName"": ""id"",
                ""desc"": true
            }
        ]");
        sb.Append($@",""limit"": {limit}");
        sb.Append(@"}");
        return sb.ToString();
    }

    static string fieldsBlock = @"""fields"": [
                ""id"",
                ""name"",
                ""status"",
                ""when"",
                ""responsible"",
                ""timeFinished"",
                ""description"",
                ""category"",
                ""timeCreated"",
                ""isDropped"",
                ""isOverdue""
            ],";
}
