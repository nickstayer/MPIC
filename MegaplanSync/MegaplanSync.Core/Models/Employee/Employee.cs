using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Deal;
using System.Text;
using System.Text.Json.Serialization;

namespace MegaplanSync.Core.Models.Employee;

public class Employee : IEquatable<Employee>, IHasId, IHasPayload
{

    public bool Equals(Employee? other)
    {
        if (other == null) return false;
        return Id == other.Id;
    }

    public override bool Equals(object obj) => Equals(obj as Employee);
    public override int GetHashCode() => (Id).GetHashCode();

    public static string GetPayload(long afterId = Consts.ID_BEFORE_START_ID, 
        int limit = Consts.JSON_ENTRIES_LIMIT, string filterId = null)
    {
        return EmployeePayload.GetPayload(afterId, limit, filterId);
    }

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "Employee";

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    [JsonPropertyName("middleName")]
    public string MiddleName { get; set; }

    [JsonPropertyName("lastName")]
    public string LastName { get; set; }

    [JsonPropertyName("position")]
    public string Position { get; set; }

    [JsonPropertyName("department")]
    public Department.Department Department { get; set; }

    [JsonPropertyName("uid")]
    public long Uid { get; set; }

    [JsonPropertyName("gender")]
    public string Gender { get; set; }

    [JsonPropertyName("birthday")]
    public DateOnlyObject Birthday { get; set; }

    [JsonPropertyName("contactInfo")]
    public List<ContactInfo> ContactInfo { get; set; }

    [JsonPropertyName("contactInfoCount")]
    public int ContactInfoCount { get; set; }

    [JsonPropertyName("isWorking")]
    public bool IsWorking { get; set; }

    [JsonPropertyName("nearestVacation")]
    public Vacation NearestVacation { get; set; }


    [JsonPropertyName("isReadable")]
    public bool IsReadable { get; set; }

    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }

    [JsonPropertyName("lastOnline")]
    public DateTimeContent LastOnline { get; set; }

    [JsonPropertyName("canLogin")]
    public bool CanLogin { get; set; }

    [JsonPropertyName("avatar")]
    public FileObject Avatar { get; set; }

    //[JsonPropertyName("hideMyBirthday")]
    //public bool HideMyBirthday { get; set; }

    //[JsonPropertyName("inn")]
    //public string Inn { get; set; }

    //[JsonPropertyName("age")]
    //public int? Age { get; set; }

    //[JsonPropertyName("address")]
    //public object Address { get; set; }

    //[JsonPropertyName("fireInProgress")]
    //public bool FireInProgress { get; set; }

    //[JsonPropertyName("possibleActions")]
    //public List<string> PossibleActions { get; set; }

    //[JsonPropertyName("settings")]
    //public List<object> Settings { get; set; }

    //[JsonPropertyName("dateLastReadNews")]
    //public string DateLastReadNews { get; set; }

    //[JsonPropertyName("availableTransports")]
    //public List<string> AvailableTransports { get; set; }

    //[JsonPropertyName("defaultReminders")]
    //public List<object> DefaultReminders { get; set; }

    //[JsonPropertyName("defaultRemindersCount")]
    //public int DefaultRemindersCount { get; set; }

    //[JsonPropertyName("emailFooter")]
    //public string EmailFooter { get; set; }

    //[JsonPropertyName("effectiveness")]
    //public int Effectiveness { get; set; }

    //[JsonPropertyName("description")]
    //public string Description { get; set; }

    //[JsonPropertyName("megaMail")]
    //public string MegaMail { get; set; }

    //[JsonPropertyName("appearanceDay")]
    //public DateTimeContent AppearanceDay { get; set; }

    //[JsonPropertyName("fireDay")]
    //public object FireDay { get; set; }

    //[JsonPropertyName("passportData")]
    //public string PassportData { get; set; }

    //[JsonPropertyName("lastAssignedTasksCount")]
    //public int LastAssignedTasksCount { get; set; }

    //[JsonPropertyName("lastClosedTasksCount")]
    //public int LastClosedTasksCount { get; set; }

    //[JsonPropertyName("lastOverdueTasksCount")]
    //public int LastOverdueTasksCount { get; set; }

    //[JsonPropertyName("lastOverdueClosedTasksCount")]
    //public int LastOverdueClosedTasksCount { get; set; }

    //[JsonPropertyName("status")]
    //public EmployeeStatus Status { get; set; }

    //[JsonPropertyName("notificationsUnreadCount")]
    //public int NotificationsUnreadCount { get; set; }

    //[JsonPropertyName("behaviour")]
    //public object Behaviour { get; set; }

    //[JsonPropertyName("login")]
    //public string Login { get; set; }

    //[JsonPropertyName("locale")]
    //public string Locale { get; set; }

    //[JsonPropertyName("timeCreated")]
    //public DateTimeContent TimeCreated { get; set; }

    //[JsonPropertyName("googleSyncSetting")]
    //public object GoogleSyncSetting { get; set; }

    //[JsonPropertyName("photo")]
    //public object Photo { get; set; }

    //[JsonPropertyName("WorkSchedule")]
    //public string WorkSchedule { get; set; }
}
