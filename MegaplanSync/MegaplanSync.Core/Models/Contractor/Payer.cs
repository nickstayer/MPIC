using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Contractor
{
    public class Payer
    {
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("country")]
        public Country? Country { get; set; }

        [JsonPropertyName("isDropped")]
        public bool? IsDropped { get; set; }

        [JsonPropertyName("bank")]
        public string? Bank { get; set; }

        [JsonPropertyName("bik")]
        public string? Bik { get; set; }

        [JsonPropertyName("inn")]
        public string? Inn { get; set; }

        [JsonPropertyName("edrpou")]
        public string? Edrpou { get; set; }

        [JsonPropertyName("correspondentAccount")]
        public string? CorrespondentAccount { get; set; }

        [JsonPropertyName("kpp")]
        public string? Kpp { get; set; }

        [JsonPropertyName("ogrn")]
        public string? Ogrn { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("legalAddress")]
        public string? LegalAddress { get; set; }

        [JsonPropertyName("currentAccount")]
        public string? CurrentAccount { get; set; }

        [JsonPropertyName("mfo")]
        public string? Mfo { get; set; }

        [JsonPropertyName("unp")]
        public string? Unp { get; set; }

        [JsonPropertyName("okpo")]
        public string? Okpo { get; set; }

        [JsonPropertyName("contractor")]
        public Contractor? Contractor { get; set; }
    }
}