using System.Text;

namespace MegaplanSync.Core.Models.Deal;

public class DealPayload
{
    public static string GetPayload(long afterId = Consts.ID_BEFORE_START_ID, 
        int limit = Consts.JSON_ENTRIES_LIMIT, string filterId = null)
    {
        // 7/4376
        var sb = new StringBuilder();

        sb.Append("{");
        sb.Append(fieldsBlock);
        sb.Append(@$"""onlyRequestedFields"": true,
        ""limit"": {limit}");

        if (filterId != null)
        {
            sb.Append(@$",
            ""filter"":
            {{
                ""id"": ""{filterId}"",
                ""contentType"":""TradeFilter""
            }}");
        }

        if (afterId != Consts.ID_BEFORE_START_ID)
        {
            sb.Append(@$",
            ""pageAfter"": 
            {{
                ""id"": ""{afterId}"",
                ""contentType"": ""Deal""
            }}");
        }

        sb.Append(@"}");

        return sb.ToString();
        //var sb = new StringBuilder();

        //sb.Append("{");
        //sb.Append(fieldsBlock);
        //sb.Append(@$"""onlyRequestedFields"": true,
        //            ""limit"": {limit}");
        //if (afterId != Consts.ID_BEFORE_START_ID)
        //{
        //    sb.Append($@",
        //        ""pageAfter"": {{
        //            ""id"": ""{afterId}"",
        //            ""contentType"": ""Deal""
        //        }}"); 
        //}
        //sb.Append(@"
        //}");

        //return sb.ToString();
    }

    public static string GetActiveDealsPayload(long afterId = Consts.ID_BEFORE_START_ID, int limit = Consts.JSON_ENTRIES_LIMIT)
    {
        var sb = new StringBuilder();

        sb.Append("{");
        sb.Append(fieldsBlock);
        sb.Append(@$"""onlyRequestedFields"": true,
        ""limit"": {limit},");
        sb.Append(@"""filter"":
    {
        ""contentType"":""TradeFilter"",
        ""id"":null,
        ""config"":{
            ""contentType"":""FilterConfig"",
            ""termGroup"":
            {
                ""contentType"":""FilterTermGroup"",
                ""join"":""and"",
                ""terms"":[{""contentType"":""FilterTermEnum"",""field"":""result"",""comparison"":""equals"",""value"":[""active""]}]
            }
        },
        ""program"":{""id"":""7"",""contentType"":""Program""}
    }");

        if (afterId != Consts.ID_BEFORE_START_ID)
        {
            sb.Append(@$",
            ""pageAfter"": {{
                ""id"": ""{afterId}"",
                ""contentType"": ""Deal""
            }}");
        }

        sb.Append(@"
    }");

        return sb.ToString();
    }

    public static string GetFiltredDealsPayload(string filterId, long afterId = Consts.ID_BEFORE_START_ID, int limit = Consts.JSON_ENTRIES_LIMIT)
    {
        // 7/4376
        var sb = new StringBuilder();

        sb.Append("{");
        sb.Append(fieldsBlock);
        sb.Append(@$"""onlyRequestedFields"": true,
        ""limit"": {limit},");

        sb.Append(@$"""filter"":
        {{""id"": ""{filterId}"",
            ""contentType"":""TradeFilter""
        }}");

        if (afterId != Consts.ID_BEFORE_START_ID)
        {
            sb.Append(@$",
            ""pageAfter"": {{
                ""id"": ""{afterId}"",
                ""contentType"": ""Deal""
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
                ""shortDescription"",
                ""contractor"",
                ""manager"",
                ""price"",
                ""state"",
                ""result"",
                ""timeCreated"",
                ""timeUpdated"",
                ""Category1000051CustomFieldOplata1"",
                ""Category1000051CustomFieldDataOplati1"",
                ""Category1000051CustomFieldFakticheskayaDataOplati1"",
                ""Category1000051CustomFieldOplata2"",
                ""Category1000051CustomFieldDataOplati2"",
                ""Category1000051CustomFieldFakticheskayaDataOplati2"",
                ""Category1000051CustomFieldDataOplati21"",
                ""Category1000051CustomFieldDataOplati3"",
                ""Category1000051CustomFieldFakticheskayaDataOplati3"",
                ""Category1000051CustomFieldDataVipolneniyaKommUsl"",
                ""Category1000051CustomFieldSmetnayaPribilSUchetomNaloga"",
                ""Category1000051CustomFieldTrudozatratiChS"",
                ""Category1000051CustomFieldOplata4"",
                ""Category1000051CustomFieldPlaniruemayaDataOplati4"",
                ""Category1000051CustomFieldFakticheskayaDataOplati31"",
                ""Category1000051CustomFieldPoluchenaOplata1"",
                ""Category1000051CustomFieldPoluchenaOplata2"",
                ""Category1000051CustomFieldPoluchenaOplata3"",
                ""Category1000051CustomFieldPoluchenaOplata4"",
                ""Category1000051CustomFieldPlaniruemayaSummaOplati"",
                ""Category1000051CustomFieldVeroyatnostUspeha"",
                ""Category1000051CustomFieldKommercheskoePredlozhenie"",
                ""Category1000051CustomFieldTipZayavki"",
                ""Category1000051CustomFieldRentabelnost"",
                ""Category1000051CustomFieldDataGotovnostiOborudovaniya"",
                ""Category1000051CustomFieldOtzivPismoPoluchen"",
                ""Category1000051CustomFieldDataPolucheniyaOtzivaPisma"",
                ""Category1000051CustomFieldVideootzivPoluchen"",
                ""Category1000051CustomFieldDataPolucheniyaVideotziva"",
                ""Category1000051CustomFieldKeysProektaOformlen"",
                ""Category1000051CustomFieldDataOformleniyaKeysa"",
                ""Category1000051CustomFieldZakrivayushchieDokumentiPeredaniVBuh"",
                ""Category1000051CustomFieldDataPeredachiZakrivayushchihDokument"",
                ""Category1000051CustomFieldRoistat""
            ],";
}
