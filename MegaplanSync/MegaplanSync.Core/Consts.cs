namespace MegaplanSync.Core;

/// <summary>
/// Константы, необходимые для работы MPIC с API Мегаплана.
/// </summary>
public class Consts
{
    public static string TOKEN_FILE_MEGAPLAN = Path.Combine("Megaplan/Token", "token.json");
    public static string TOKEN_EXP_AT_FILE_MEGAPLAN = Path.Combine("Megaplan/Token", "token_exp_at.txt");

    public const int JSON_ENTRIES_LIMIT = 100;
    public const long ID_BEFORE_START_ID = 0;

    // как в api
    public const string ENTITY_NAME_DEAL = "deal";

    public static string MAPPING_DEAL_RULES_FILE = Path.Combine("Megaplan/Mapping", "deal_mapping_rules.json");
}
