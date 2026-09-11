namespace MegaplanSync.Core;

public class Consts
{
    public static string TOKEN_FILE_MEGAPLAN = Path.Combine("Megaplan/Token", "token.json");
    public static string TOKEN_EXP_AT_FILE_MEGAPLAN = Path.Combine("Megaplan/Token", "token_exp_at.txt");
    public static string APP_SETTINGS_FILE = Path.Combine("Megaplan", "appsettings.json");
    
    public const string TEST_FOLDER_NAME = "testdata";

    //public const string BASE_API_URL = "https://crm.chelzeo.ru/api/v3";
    //public const string USERNAME = "autodeal@chelzeo.ru";
    //public const string PASSWORD = "IgbfddfgKJbfgf34";
    //public const string USERNAME = "velichkina@chelzeo.ru";
    //public const string PASSWORD = "GuhP12r?";
    //public const string CONNECTION_STRING_LOCAL = "server=localhost;user id=megauser;password=23jfmnslSdf4;database=mp_chzeo";

    public const string CONNECTION_STRING_REMOTE = "server=192.168.1.201;user id=megauser;password=23jfmnslSdf4;database=mp_chzeo";    
    public const string CONNECTION_STRING_TEST = "server=localhost;user id=megauser;password=23jfmnslSdf4;database=mp_chzeo_test";

    public const int JSON_ENTRIES_LIMIT = 100;
    public const long ID_BEFORE_START_ID = 0;
    public static double DEFAULT_DELAY_HOURS = 2;
    
    // как в бд
    public const string TABLE_NAME_DEAL = "deals";
    public const string TABLE_NAME_DEAL_HISTORY_STATUS = "dealstatushistory";
    public const string TABLE_NAME_CONTRACTOR = "contractor";
    public const string TABLE_NAME_EMPLOYEE = "employee";
    public const string TABLE_NAME_DEPARTMENT = "departments";
    public const string TABLE_NAME_TODO = "todos";
    public const string TABLE_NAME_TODO_CATEGORY = "todocategories";
    public const string TABLE_NAME_TODO_STATUS = "todostatuses";

    // как в api
    public const string ENTITY_NAME_TODO = "todo";
    public const string ENTITY_NAME_TODO_CATEGORY = "todoCategory";
    public const string ENTITY_NAME_TODO_STATUS = "todoStatus";
    public const string ENTITY_NAME_DEAL = "deal";
    public const string ENTITY_NAME_CALLINFO = "callInfo";
    public const string ENTITY_NAME_DEAL_HISTORY_STATUS = "statusHistory";
    public const string ENTITY_NAME_CONTRACTOR = "contractor";
    public const string ENTITY_NAME_EMPLOYEE = "employee";
    public const string ENTITY_NAME_DEPARTMENT = "department";

    public static string MAPPING_DEAL_RULES_FILE = Path.Combine("Megaplan/Mapping", "deal_mapping_rules.json");
    public static string MAPPING_EMPLOYEE_RULES_FILE = Path.Combine("Megaplan/Mapping", "employee_mapping_rules.json");

    // настройки при массовых операциях
    public const int DELAY_PER_REQUEST_MS = 150;
    public const int MAX_CONCURRENCY = 10;
}
