using System.Text;

namespace MegaplanSync.Core.Models.Employee;

public class EmployeePayload
{
    public static string GetPayload(long afterId = Consts.ID_BEFORE_START_ID, 
        int limit = Consts.JSON_ENTRIES_LIMIT, string filterId = null)
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
                ""contentType"": ""Employee""
            }}");
        }

        sb.Append(@"}");

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
                ""fieldName"": ""id"",
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
                ""lastName"",
                ""position"",
                ""department"",
                ""uid"",
                ""gender"",
                ""birthday"",
                ""contactInfo"",
                ""contactInfoCount"",
                ""isWorking"",
                ""nearestVacation"",
                ""isReadable"",
                ""isOnline"",
                ""lastOnline"",
                ""canLogin"",
                ""avatar""
            ],";

    //sb.Append(@"""sortBy"": [
    //    {
    //        ""contentType"": ""SortField"",
    //        ""fieldName"": ""timeCreated"",
    //        ""desc"": false
    //    }
    //]");



    //public static string GetPayload(long afterId)
    //{
    //    var sb = new StringBuilder();

    //    sb.Append(@"{
    //        ""filter"": {
    //            ""id"": ""all"",
    //            ""contentType"": ""EmployeeFilter""
    //        },
    //        ""fields"": [
    //            ""isOnline"",
    //            ""avatar"",
    //            ""canLogin"",
    //            ""contactInfo"",
    //            ""isOnline"",
    //            ""isReadable"",
    //            ""isWorking"",
    //            ""name"",
    //            ""nearestVacation"",
    //            ""position"",
    //            ""login""
    //        ],
    //        ""onlyRequestedFields"": true,
    //        ""sortBy"": [
    //            {
    //                ""contentType"": ""SortField"",
    //                ""fieldName"": ""id"",
    //                ""desc"": false
    //            }
    //        ],
    //        ""limit"": 100");

    //    sb.Append($@",
    //        ""pageAfter"": {{
    //            ""id"": ""{afterId}"",
    //            ""contentType"": ""Employee""
    //        }}");

    //    sb.Append(@"
    //    }");

    //    return sb.ToString();
    //}
}
