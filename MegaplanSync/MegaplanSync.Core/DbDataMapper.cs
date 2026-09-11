using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models;
using MegaplanSync.Core.Models.Contractor;
using MegaplanSync.Core.Models.Department;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.Employee;
using System.Data;
using System.Globalization;

namespace MegaplanSync.Core;

public class DbDataMapper(ILogger logger) : IDbDataMapper
{
    public List<Deal> MapDeals(DataTable dataTable)
    {
        var result = new List<Deal>();

        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            logger?.LogWarning("Получена пустая или null DataTable для маппинга.");
            return result;
        }

        var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
        foreach (DataRow row in dataTable.Rows)
        {
            var deal = MapDealFromDataRow(row);
            normalizer.Normalize(deal);
            result.Add(deal);
        }

        return result;
    }

    public List<Department> MapDepartments(DataTable dataTable)
    {
        var result = new List<Department>();

        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            logger?.LogWarning("Получена пустая или null DataTable для маппинга.");
            return result;
        }
        foreach (DataRow row in dataTable.Rows)
        {
            var department = MapDepartmentFromDataRow(row);
            result.Add(department);
        }

        return result;
    }

    private Department MapDepartmentFromDataRow(DataRow row)
    {
        var department = new Department();
        var deserializer = new JsonHelper(logger);
        department.Id = GetColumnValue<string>(row, "id");
        department.Name = GetColumnValue<string>(row, "name") == "0" ? null
            : GetColumnValue<string>(row, "name");
        return department;
    }

    private Deal MapDealFromDataRow(DataRow row)
    {
        var deal = new Deal();
        var deserializer = new JsonHelper(logger);

        deal.Id = GetColumnValue<string>(row, "id");

        deal.Number = GetColumnValue<string>(row, "number") == "0" ? null
            : GetColumnValue<string>(row, "number");

        deal.Name = GetColumnValue<string>(row, "name") == "0" ? null :
            GetColumnValue<string>(row, "name");

        deal.ShortDescription = GetColumnValue<string>(row, "shortDescription") == "0" ? null
            : GetColumnValue<string>(row, "shortDescription");

        deal.Result = GetColumnValue<string>(row, "result") == "0" ? null
            : GetColumnValue<string>(row, "result");

        deal.TipZayavki = GetColumnValue<string>(row, "Category1000051CustomFieldTipZayavki") == "0" ? null
            : GetColumnValue<string>(row, "Category1000051CustomFieldTipZayavki");

        deal.Contractor = deserializer.DeserializeOrDefault<Contractor>(row, "contractor");
        deal.Manager = deserializer.DeserializeOrDefault<Employee>(row, "manager");

        deal.Price = deserializer.DeserializeOrDefault<Money>(row, "price");
        deal.State = deserializer.DeserializeOrDefault<ProgramState>(row, "state");

        deal.TimeCreated = deserializer.DeserializeOrDefault<DateTimeObject>(row, "timeCreated");
        deal.TimeUpdated = deserializer.DeserializeOrDefault<DateTimeObject>(row, "timeUpdated");

        deal.Oplata1 = deserializer.DeserializeOrDefault<Money>(row, "Category1000051CustomFieldOplata1");
        deal.DataOplati1 = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldDataOplati1");
        deal.FakticheskayaDataOplati1 = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldFakticheskayaDataOplati1");

        deal.Oplata2 = deserializer.DeserializeOrDefault<Money>(row, "Category1000051CustomFieldOplata2");
        deal.DataOplati2 = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldDataOplati2");
        deal.FakticheskayaDataOplati2 = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldFakticheskayaDataOplati2");

        deal.Oplata3 = deserializer.DeserializeOrDefault<Money>(row, "Category1000051CustomFieldDataOplati21"); // "косяк разработчиков"
        deal.DataOplati3 = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldDataOplati3");
        deal.FakticheskayaDataOplati3 = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldFakticheskayaDataOplati3");

        deal.Oplata4 = deserializer.DeserializeOrDefault<Money>(row, "Category1000051CustomFieldOplata4");
        deal.PlaniruemayaDataOplati4 = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldPlaniruemayaDataOplati4");
        deal.FakticheskayaDataOplati4 = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldFakticheskayaDataOplati31");

        deal.DataVipolneniyaKommUsl = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldDataVipolneniyaKommUsl");
        deal.SmetnayaPribilSUchetomNaloga = deserializer.DeserializeOrDefault<Money>(row, "Category1000051CustomFieldSmetnayaPribilSUchetomNaloga");

        deal.TrudozatratiChS = GetColumnValue<double>(row, "Category1000051CustomFieldTrudozatratiChS");
        deal.PoluchenaOplata1 = GetBoolFromDbValue(row, "Category1000051CustomFieldPoluchenaOplata1");
        deal.PoluchenaOplata2 = GetBoolFromDbValue(row, "Category1000051CustomFieldPoluchenaOplata2");
        deal.PoluchenaOplata3 = GetBoolFromDbValue(row, "Category1000051CustomFieldPoluchenaOplata3");
        deal.PoluchenaOplata4 = GetBoolFromDbValue(row, "Category1000051CustomFieldPoluchenaOplata4");

        deal.PlaniruemayaSummaOplati = deserializer.DeserializeOrDefault<Money>(row, "Category1000051CustomFieldPlaniruemayaSummaOplati");
        deal.VeroyatnostUspeha = GetColumnValue<int>(row, "Category1000051CustomFieldVeroyatnostUspeha");

        deal.KommercheskoePredlozhenie = deserializer.DeserializeListOrDefault<FileObject>(row, "Category1000051CustomFieldKommercheskoePredlozhenie");

        deal.Rentabelnost = GetColumnValue<double>(row, "Category1000051CustomFieldRentabelnost");
        deal.DataGotovnostiOborudovaniya = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldDataGotovnostiOborudovaniya");
        deal.OtzivPismoPoluchen = GetBoolFromDbValue(row, "Category1000051CustomFieldOtzivPismoPoluchen");
        deal.DataPolucheniyaOtzivaPisma = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldDataPolucheniyaOtzivaPisma");
        deal.VideootzivPoluchen = GetBoolFromDbValue(row, "Category1000051CustomFieldVideootzivPoluchen");
        deal.DataPolucheniyaVideotziva = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldDataPolucheniyaVideotziva");
        deal.KeysProektaOformlen = GetBoolFromDbValue(row, "Category1000051CustomFieldKeysProektaOformlen");
        deal.DataOformleniyaKeysa = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldDataOformleniyaKeysa");
        deal.ZakrivayushchieDokumentiPeredaniVBuh = GetBoolFromDbValue(row, "Category1000051CustomFieldZakrivayushchieDokumentiPeredaniVBuh");
        deal.DataPeredachiZakrivayushchihDokument = deserializer.DeserializeOrDefault<DateOnlyObject>(row, "Category1000051CustomFieldDataPeredachiZakrivayushchihDokument");


        return deal;
    }

    private bool GetBoolFromDbValue(DataRow row, string columnName)
    {
        var stringValue = GetColumnValue<string>(row, columnName);

        if (stringValue == "1")
        {
            return true;
        }
        else if (stringValue == "0")
        {
            return false;
        }
        return false;
    }

    /// <summary>
    /// Вспомогательный метод для безопасного получения значения колонки из DataRow.
    /// Обрабатывает DBNull и возвращает значение по умолчанию для типа T.
    /// </summary>
    private T? GetColumnValue<T>(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName) || row.IsNull(columnName))
        {
            return default(T);
        }

        var value = row[columnName];
        Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType == typeof(bool))
        {
            try
            {
                if (value is int intValue)
                {
                    return (T)(object)(intValue != 0);
                }
                if (value is string stringValue)
                {
                    if (int.TryParse(stringValue, out int parsedInt))
                    {
                        return (T)(object)(parsedInt != 0);
                    }
                    if (bool.TryParse(stringValue, out bool parsedBool))
                    {
                        return (T)(object)parsedBool;
                    }
                }
                return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Не удалось привести значение '{value}' из колонки '{columnName}' к типу 'Boolean'. Ошибка: {ex.Message}");
                return default(T);
            }
        }
        else
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            try
            {
                return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Не удалось привести значение '{value}' из колонки '{columnName}' к типу '{typeof(T).Name}'. Ошибка: {ex.Message}");
                return default(T);
            }
        }
    }
}