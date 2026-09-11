using MegaplanSync.Core;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models;
using MegaplanSync.Core.Models.Contractor;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.Department;
using MegaplanSync.Core.Models.Employee;
using MegaplanSync.DataAccess.MySql;
using MegaplanSync.Service;
using Moq;

namespace MegaplanSync.Tests;

public class DbServiceTests
{
    ILogger logger;
    IDataAccess dataAccess;
    IDbDataMapper dbDataMapper;
    DbService dbService;
    AppSettings appSettings;

    [SetUp]
    public void Setup()
    {
        logger = new Mock<ILogger>().Object;
        var serializer = new JsonHelper(logger);
        appSettings = serializer.LoadEntityFromFile<AppSettings>(Consts.APP_SETTINGS_FILE);
        if (appSettings?.LaunchTime == null
            || appSettings.LaunchTime.Length == 0
            || string.IsNullOrWhiteSpace(appSettings.Username)
            || string.IsNullOrWhiteSpace(appSettings.Password)
            || string.IsNullOrWhiteSpace(appSettings.BaseApUrl)
            || string.IsNullOrWhiteSpace(appSettings.ConnectionString))
        {
            throw new Exception("Ошибка: некорректные настройки.");
        }
        dataAccess = new MySqlDataAccess(logger, Consts.CONNECTION_STRING_TEST);
        dbDataMapper = new DbDataMapper(logger);
        dbService = new(logger, dataAccess, dbDataMapper);
    }

    [Test]
    public async Task IsDealStatusHistoryExistsTest()
    {
        var dataAccess = new MySqlDataAccess(logger, appSettings.ConnectionString);
        DbService dbService = new(logger, dataAccess, dbDataMapper);
        var actual1 = await dbService.IsDealStatusHistoryExists((new Core.Models.DealStatusHistory.DealStatusHistory { Id = "130377" }, 111));
        var expected1 = true;
        var actual2 = await dbService.IsDealStatusHistoryExists((new Core.Models.DealStatusHistory.DealStatusHistory { Id = "23" }, 111));
        var expected2 = false;
        Assert.That(actual1, Is.EqualTo(expected1));
        Assert.That(actual2, Is.EqualTo(expected2));
    }

    [Test]
    public async Task InsertDealTest()
    {
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Consts.TEST_FOLDER_NAME}\\InsertDealTest.json");
        if (!File.Exists(jsonFile)) throw new FileNotFoundException(jsonFile);
        var dealsFromFile = JsonHelper.LoadDealsFromFile(jsonFile);
        var actual = 0;
        foreach (var deal in dealsFromFile)
        {
            actual += await dbService.Insert(deal, Consts.TABLE_NAME_DEAL);

        }
        var expected = 1;
        Assert.That(actual, Is.EqualTo(expected));
    }
    

    [Test]
    public async Task InsertContractorTest()
    {
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_CONTRACTOR);
        var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Consts.TEST_FOLDER_NAME}\\InsertContractorTest.json");
        if (!File.Exists(jsonFile)) throw new FileNotFoundException(jsonFile);
        var jsonHelper = new JsonHelper();
        var contractorsFromFile = jsonHelper.LoadEntitiesFromFile<Contractor>(jsonFile);
        var actual = 0;
        foreach (var contractor in contractorsFromFile)
        {
            actual += await dbService.Insert(contractor, Consts.TABLE_NAME_CONTRACTOR);
        }
        var expected = 2;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task InsertEmployeeTest()
    {
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_EMPLOYEE);
        var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Consts.TEST_FOLDER_NAME}\\InsertEmployeeTest.json");
        if (!File.Exists(jsonFile)) throw new FileNotFoundException(jsonFile);
        var deserializer = new JsonHelper();
        var employeesFromFile = deserializer.LoadEntitiesFromFile<Employee>(jsonFile);
        var actual = 0;
        foreach (var employee in employeesFromFile)
        {
            actual += await dbService.Insert(employee, Consts.TABLE_NAME_EMPLOYEE);
        }
        var expected = 1;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetAndMapLastDealTest()
    {
        await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        long id1 = 124;
        long id2 = 125;
        long id3 = 122;
        var testDeal1 = new Deal { Id = $"{id1}" };
        var testDeal2 = new Deal { Id = $"{id2}" };
        var testDeal3 = new Deal { Id = $"{id3}" };
        await dbService.Insert(testDeal1, Consts.TABLE_NAME_DEAL);
        await dbService.Insert(testDeal2, Consts.TABLE_NAME_DEAL);
        await dbService.Insert(testDeal3, Consts.TABLE_NAME_DEAL);
        var lastDealFromDB = await dbService.GetAndMapLastDeal();
        var actual = long.Parse(lastDealFromDB.Id) == id2;
        var expected = true;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetLastUpdateDateFromDbLogTest()
    {
        var dateObject = await dbService.GetLastUpdateDateFromDbLog();
        var expected = true;
        var actual = dateObject != default;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetLastDealIdTest()
    {
        await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        long id1 = 124;
        var testDeal1 = new Deal { Id = $"{id1}" };
        await dbService.Insert(testDeal1, Consts.TABLE_NAME_DEAL);
        var lastDealFromDB = await dbService.GetLastId(Consts.TABLE_NAME_DEAL);
        var actual = lastDealFromDB == id1;
        var expected = true;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetLastContratorIdTest()
    {
        await dbService.DeleteAllEntries(Consts.TABLE_NAME_CONTRACTOR);
        long id1 = 124;
        var contractor = new Contractor { Id = $"{id1}" };
        await dbService.Insert(contractor, Consts.TABLE_NAME_CONTRACTOR);
        var lastContractorFromDB = await dbService.GetLastId(Consts.TABLE_NAME_CONTRACTOR);
        var actual = lastContractorFromDB == id1;
        var expected = true;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetLastEmployeeIdTest()
    {
        await dbService.DeleteAllEntries(Consts.TABLE_NAME_EMPLOYEE);
        long id1 = 124;
        var employee = new Employee { Id = $"{id1}" };
        await dbService.Insert(employee, Consts.TABLE_NAME_EMPLOYEE);

        var lastContractorFromDB = await dbService.GetLastId(Consts.TABLE_NAME_EMPLOYEE);
        var actual = lastContractorFromDB == id1;
        var expected = true;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetLastDepartmentIdTest()
    {
        await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEPARTMENT);
        long id1 = 124;
        var entry = new Department { Id = $"{id1}" };
        await dbService.Insert(entry, Consts.TABLE_NAME_DEPARTMENT);

        var lastIdFromDB = await dbService.GetLastId(Consts.TABLE_NAME_DEPARTMENT);
        var actual = lastIdFromDB == id1;
        var expected = true;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetAndMapLastDealIdNullTest()
    {
        await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        long id1 = 124;
        var lastDealFromDB = await dbService.GetLastId(Consts.TABLE_NAME_DEAL);
        var actual = lastDealFromDB == 0;
        var expected = true;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetAndMapSpecifiedDealTest()
    {
        long id = 121;
        var testDeal = new Deal
        {
            Id = $"{id}",
            Number = "57",
            Name = "Тестовая сделка для вставки",
            ShortDescription = "Краткое описание тестовой сделки.",
            Result = "Успех",
            Contractor = new Contractor { Id = "contr_1", Name = "Тестовый Контрагент" },
            Manager = new Employee { Id = "emp_1", Name = "Тестовый Менеджер" },
            Price = new Money { Value = 1000, Currency = "RUB" },
            State = new ProgramState { Id = "state_1", Name = "В работе" },
            TimeCreated = new DateTimeObject { Value = DateTime.UtcNow.AddDays(-5) },
            TimeUpdated = new DateTimeObject { Value = DateTime.UtcNow },
            Oplata1 = new Money { Value = 500, Currency = "USD" },
            DataOplati1 = new DateOnlyObject { Day = 29, Month = 7, Year = 2025 },
            TrudozatratiChS = 42.5,
            VeroyatnostUspeha = 95,
            PoluchenaOplata1 = true,
            OtzivPismoPoluchen = false,
            Rentabelnost = 0.25,
            PlaniruemayaSummaOplati = new Money { Value = 1000, Currency = "RUB" },
        };
        await dbService.DeleteEntity(id, Consts.TABLE_NAME_DEAL);
        await dbService.Insert(testDeal, Consts.TABLE_NAME_DEAL);
        var deal = await dbService.GetAndMapDeal(id);
        var actual = deal.Price.Value;
        var expected = (decimal)1000;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TearDown]
    public void TearDown() 
    {
        dataAccess.Dispose();
    }
}
