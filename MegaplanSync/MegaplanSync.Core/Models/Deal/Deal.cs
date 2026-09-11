using MegaplanSync.Core.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Deal;

public class Deal : IHasId, IHasPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("shortDescription")]
    public string ShortDescription { get; set; }

    [JsonPropertyName("contractor")]
    public Contractor.Contractor? Contractor { get; set; }

    [JsonPropertyName("manager")]
    public Employee.Employee? Manager { get; set; }

    [JsonPropertyName("price")]
    public Money Price { get; set; }

    [JsonPropertyName("state")]
    public ProgramState State { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; }

    [JsonPropertyName("timeCreated")]
    public DateTimeObject TimeCreated { get; set; }

    [JsonPropertyName("timeUpdated")]
    public DateTimeObject TimeUpdated { get; set; }

    [JsonPropertyName("Category1000051CustomFieldOplata1")]
    public Money? Oplata1 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldDataOplati1")]
    public DateOnlyObject? DataOplati1 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldFakticheskayaDataOplati1")]
    public DateOnlyObject? FakticheskayaDataOplati1 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldOplata2")]
    public Money? Oplata2 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldDataOplati2")]
    public DateOnlyObject? DataOplati2 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldFakticheskayaDataOplati2")]
    public DateOnlyObject? FakticheskayaDataOplati2 { get; set; }

    // косяк разработчиков
    [JsonPropertyName("Category1000051CustomFieldDataOplati21")]
    public Money? Oplata3 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldDataOplati3")]
    public DateOnlyObject? DataOplati3 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldFakticheskayaDataOplati3")]
    public DateOnlyObject? FakticheskayaDataOplati3 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldOplata4")]
    public Money? Oplata4 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldPlaniruemayaDataOplati4")]
    public DateOnlyObject? PlaniruemayaDataOplati4 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldFakticheskayaDataOplati31")]
    public DateOnlyObject? FakticheskayaDataOplati4 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldDataVipolneniyaKommUsl")]
    public DateOnlyObject? DataVipolneniyaKommUsl { get; set; }

    [JsonPropertyName("Category1000051CustomFieldSmetnayaPribilSUchetomNaloga")]
    public Money? SmetnayaPribilSUchetomNaloga { get; set; }

    [JsonPropertyName("Category1000051CustomFieldTrudozatratiChS")]
    public double? TrudozatratiChS { get; set; }

    [JsonPropertyName("Category1000051CustomFieldPoluchenaOplata1")]
    [JsonConverter(typeof(NullableBoolConverter))]
    public bool? PoluchenaOplata1 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldPoluchenaOplata2")]
    [JsonConverter(typeof(NullableBoolConverter))]
    public bool? PoluchenaOplata2 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldPoluchenaOplata3")]
    [JsonConverter(typeof(NullableBoolConverter))]
    public bool? PoluchenaOplata3 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldPoluchenaOplata4")]
    [JsonConverter(typeof(NullableBoolConverter))]
    public bool? PoluchenaOplata4 { get; set; }

    [JsonPropertyName("Category1000051CustomFieldPlaniruemayaSummaOplati")]
    public Money? PlaniruemayaSummaOplati { get; set; }

    [JsonPropertyName("Category1000051CustomFieldVeroyatnostUspeha")]
    public int? VeroyatnostUspeha { get; set; }

    [JsonPropertyName("Category1000051CustomFieldKommercheskoePredlozhenie")]
    public List<FileObject>? KommercheskoePredlozhenie { get; set; }

    [JsonPropertyName("Category1000051CustomFieldTipZayavki")]
    public string? TipZayavki { get; set; }

    [JsonPropertyName("Category1000051CustomFieldRentabelnost")]
    public double? Rentabelnost { get; set; }

    [JsonPropertyName("Category1000051CustomFieldDataGotovnostiOborudovaniya")]
    public DateOnlyObject? DataGotovnostiOborudovaniya { get; set; }

    [JsonPropertyName("Category1000051CustomFieldOtzivPismoPoluchen")]
    [JsonConverter(typeof(NullableBoolConverter))]
    public bool? OtzivPismoPoluchen { get; set; }

    [JsonPropertyName("Category1000051CustomFieldDataPolucheniyaOtzivaPisma")]
    public DateOnlyObject? DataPolucheniyaOtzivaPisma { get; set; }

    [JsonPropertyName("Category1000051CustomFieldVideootzivPoluchen")]
    [JsonConverter(typeof(NullableBoolConverter))]
    public bool? VideootzivPoluchen { get; set; }

    [JsonPropertyName("Category1000051CustomFieldDataPolucheniyaVideotziva")]
    public DateOnlyObject? DataPolucheniyaVideotziva { get; set; }

    [JsonPropertyName("Category1000051CustomFieldKeysProektaOformlen")]
    [JsonConverter(typeof(NullableBoolConverter))]
    public bool? KeysProektaOformlen { get; set; }

    [JsonPropertyName("Category1000051CustomFieldDataOformleniyaKeysa")]
    public DateOnlyObject? DataOformleniyaKeysa { get; set; }

    [JsonPropertyName("Category1000051CustomFieldZakrivayushchieDokumentiPeredaniVBuh")]
    [JsonConverter(typeof(NullableBoolConverter))]
    public bool? ZakrivayushchieDokumentiPeredaniVBuh { get; set; }

    [JsonPropertyName("Category1000051CustomFieldDataPeredachiZakrivayushchihDokument")]
    public DateOnlyObject? DataPeredachiZakrivayushchihDokument { get; set; }

    [NotMapped]
    [JsonPropertyName("Category1000051CustomFieldRoistat")]
    public string? Roistat { get; set; }

    public static string GetPayload(long afterId = Consts.ID_BEFORE_START_ID, 
        int limit = Consts.JSON_ENTRIES_LIMIT, string filterId = null)
    {
        return DealPayload.GetPayload(afterId, limit, filterId);
    }
}