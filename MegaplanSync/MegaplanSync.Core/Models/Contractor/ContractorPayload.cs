using System.Collections.Generic;
using System.Text;

namespace MegaplanSync.Core.Models.Contractor;

public class ContractorPayload
{
    public static string GetPayload(long afterId = Consts.ID_BEFORE_START_ID, int limit = Consts.JSON_ENTRIES_LIMIT)
    {
        var sb = new StringBuilder();

        sb.Append("{");
        sb.Append(fieldsBlock);
        sb.Append(@$"""onlyRequestedFields"": true,
        ""limit"": {limit},");
        sb.Append(@"""sortBy"": [
            {
                ""contentType"": ""SortField"",
                ""fieldName"": ""timeCreated"",
                ""desc"": false
            }
        ]");

        if (afterId != Consts.ID_BEFORE_START_ID)
        {
            sb.Append(@$",
            ""pageAfter"": {{
                ""id"": ""{afterId}"",
                ""contentType"": ""ContractorCompany""
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
        sb.Append(@"""onlyRequestedFields"": true,");
        sb.Append(@"""sortBy"": [
            {
                ""contentType"": ""SortField"",
                ""fieldName"": ""timeCreated"",
                ""desc"": true
            }
        ],");
        sb.Append($@"""limit"": {limit}");
        sb.Append(@"}");
        return sb.ToString();
    }

    static string fieldsBlock = @"""fields"": [
                ""id"",
                ""name"",
                ""firstName"",
                ""middleName"",
                ""lastName""
            ],";
}
