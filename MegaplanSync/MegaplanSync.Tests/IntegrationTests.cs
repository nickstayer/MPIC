using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.Department;
using MegaplanSync.Core.Models.Employee;
using MegaplanSync.Core.Models.Todo;
using MegaplanSync.DataAccess.MySql;
using MegaplanSync.Logging;
using MegaplanSync.Service;
using Moq;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;


namespace MegaplanSync.Tests;

public class IntegrationTests
{
    ILogger logger;
    IDataAccess dataAccess;
    IDbDataMapper dbDataMapper;
    DbService dbService;
    IApiClient apiClient;
    IApiDataMapper apiDataMapper;
    ApiService apiService;
    int entitiesCount = 4;
    Worker worker;
    string dealsNotExistsInMegaplanFile = "dealsNotExistsInMegaplan.txt";
    string headers = "id;number";
    AppSettings appSettings;

    [SetUp]
    public async Task Setup()
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
        apiDataMapper = new ApiDataMapper(logger);
        apiClient = new MegaApiClient(logger: logger, tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
        tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN, baseApiUrl: appSettings.BaseApUrl, username: appSettings.Username, password: appSettings.Password);
        apiService = new ApiService(logger, apiClient, apiDataMapper);
        worker = new Worker(apiService, dbService, logger);
    }

    // проверить почему сделка не обновилась в бд 36595
    [Test]
    [Ignore("сделка с негативным результатом не обновится")]
    public async Task CrossDbInsertDealTest()
    {
        // проверка, что нет отличий у объекта из оригинальной БД и тестовой БД

        // !!! начал работу с боевой БД
        var id = 36595;
        var dataAccessOriginal = new MySqlDataAccess(logger, Consts.CONNECTION_STRING_REMOTE);
        var dbDataMapperOriginal = new DbDataMapper(logger);
        DbService dbServiceOriginal = new(logger, dataAccessOriginal, dbDataMapperOriginal);
        var dealFromOriginalDb = await dbServiceOriginal.GetAndMapDeal(id);
        dataAccessOriginal.Dispose();
        // !!! окончил работу с боевой БД

        var deletedRowsCount = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        var dealFromApi = await apiService.GetAndMapDeal(id);
        var insetredRowsCount = await dbService.Insert(dealFromApi, Consts.TABLE_NAME_DEAL);
        var dealFromTestDb = await dbService.GetAndMapDeal(id);


        var diffs = Logic.GetDifferences(dealFromTestDb, dealFromOriginalDb);
        var actual = diffs.Count();
        var expected = 0;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task InsertDealHistoryStatusTest()
    {
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL_HISTORY_STATUS);
        var id = 36414;
        var statuses = await apiService.GetAndMapDealStatusHystoryWithDealId(id);
        var actual = 0;
        foreach (var status in statuses)
        {
            actual += await dbService.InsertDealStatusHistory(status);

        }
        var expected = statuses.Count;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task MappingTest()
    {
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        var rowsInserted = await dataAccess.ExecuteAsync(_isertTestDealQuery);
        string pattern = @"(?<=values\(')(\d+)";
        long id;
        System.Text.RegularExpressions.Match match = Regex.Match(_isertTestDealQuery, pattern);

        if (match.Success)
        {
            id = long.Parse(match.Value);
        }
        else { throw new Exception(); }
        var dealFromDb = await dbService.GetAndMapDeal(id);
        var dealFromApi = await apiService.GetAndMapDeal(id);
        var diffs = Logic.GetDifferences(dealFromDb, dealFromApi);
        var actual = diffs.Count();
        var expected = 0;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task DataNormalizerTest()
    {
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        var id = 36414;
        var _mappedFromApiDeal = await apiService.GetAndMapDeal(id);
        var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Consts.TEST_FOLDER_NAME}\\DataNormalizerTest.json");
        JsonHelper.SaveEntitiesToFile([_mappedFromApiDeal], jsonFile);

        if (!File.Exists(jsonFile)) throw new FileNotFoundException(jsonFile);
        var _deaserializedFromFileDeal = JsonHelper.LoadDealsFromFile(jsonFile).FirstOrDefault();

        await dbService.Insert(_deaserializedFromFileDeal, Consts.TABLE_NAME_DEAL);
        var _mappedFromDbDeal = await dbService.GetAndMapDeal(id);
        
        var dealFromApi = await apiService.GetAndMapDeal(long.Parse(_deaserializedFromFileDeal.Id));

        var diffJsonAndApi = Logic.GetDifferences(_deaserializedFromFileDeal, _mappedFromApiDeal);
        var diffJsonAndDb = Logic.GetDifferences(_deaserializedFromFileDeal, _deaserializedFromFileDeal);
        var diffApiAndDb = Logic.GetDifferences(_mappedFromApiDeal, _mappedFromDbDeal);
        var actual = (diffJsonAndApi.Count + diffJsonAndDb.Count + diffApiAndDb.Count) == 0;
        var expected = true;
        Assert.That(actual, Is.EqualTo(expected));
    }

    
    [Test]
    public async Task AddNewTodosToDbTest()
    {
        // создание ситуации, когда в CRM есть entitiesCount - 1 сделок с id больше, чем в БД
        var jsonPayload = TodoPayload.GetLastEntitiesPayload(entitiesCount);
        var entities = await apiService.GetAndMapEntities<Todo>(Consts.ENTITY_NAME_TODO,
            jsonPayload);
        Assert.That(entities, Is.Not.Empty);
        var oldestEntityId = Logic.GetOldestId(entities);
        var oldestEntity = entities.Where(x => x.Id == oldestEntityId.ToString()).FirstOrDefault();
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_TODO);
        var rowsInserted = await dbService.Insert(oldestEntity, Consts.TABLE_NAME_TODO);
        var actual = await worker.AddNewTodosToDb();
        var expected = entitiesCount - 1;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddNewDealsToDbTest()
    {
        // создание ситуации, когда в CRM есть entitiesCount - 1 сделок с id больше, чем в БД
        var jsonPayload = DealPayload.GetLastEntitiesPayload(entitiesCount);
        var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
        var deals = await apiService.GetAndMapEntities<Deal>(Consts.ENTITY_NAME_DEAL,
            jsonPayload, normalizer);
        if (deals.Count == 0) { throw new Exception(); }
        var oldestDealId = Logic.GetOldestId(deals);
        var oldestDeal = deals.Where(deal => deal.Id == oldestDealId.ToString()).FirstOrDefault();
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        var rowsInserted = await dbService.Insert(oldestDeal, Consts.TABLE_NAME_DEAL);

        var actual = await worker.AddNewDealsToDb();
        var expected = entitiesCount - 1;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddNewEmployeesToDbTest()
    {
        // создание ситуации, когда в CRM есть entitiesCount - 1 записей с id больше, чем в БД
        var jsonPayload = EmployeePayload.GetLastEntitiesPayload(entitiesCount);
        var entities = await apiService.GetAndMapEntities<Employee>(Consts.ENTITY_NAME_EMPLOYEE,
            jsonPayload);
        //var jsonPayload = EmployeePayload.GetPayload(); ;
        //var entities = await apiService.GetAndMapAllEntities<Employee>(Consts.ENTITY_NAME_EMPLOYEE,
        //    jsonPayload);

        Assert.That(entities, Is.Not.Empty);
        var oldestId = Logic.GetOldestId(entities);
        var oldestEntity = entities.Where(x => x.Id == oldestId.ToString()).FirstOrDefault();
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_EMPLOYEE);
        var rowsInserted = await dbService.Insert(oldestEntity, Consts.TABLE_NAME_EMPLOYEE);

        var actual = await worker.AddNewEmployeesToDb();
        var expected = entitiesCount - 1;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddNewDepartmentsToDbTest()
    {
        // подготовка БД
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEPARTMENT);
        var allEntitiesFromApi = await apiService.GetAndMapEntitiesWithoutPayload<Department>(Consts.ENTITY_NAME_DEPARTMENT);
        Assert.That(allEntitiesFromApi, Is.Not.Empty);
        var newestId = allEntitiesFromApi.OrderByDescending(x => long.Parse(x.Id)).FirstOrDefault()?.Id;
        var insertedEntities = allEntitiesFromApi.Where(x => x.Id != newestId).ToList();
        var insertedRowsCount = 0;
        foreach (var entity in insertedEntities) 
        {
            insertedRowsCount += await dbService.Insert(entity, Consts.TABLE_NAME_DEPARTMENT);
        }

        var actual = await worker.AddNewDepartmentsToDb();
        var expected = 1;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddNewTodoStatusToDbTest()
    {
        // подготовка БД
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_TODO_STATUS);
        var allEntitiesFromApi = await apiService.GetAndMapEntitiesWithoutPayload<TodoStatus>(Consts.ENTITY_NAME_TODO_STATUS);
        Assert.That(allEntitiesFromApi, Is.Not.Empty);
        var newestId = allEntitiesFromApi.OrderByDescending(x => long.Parse(x.Id)).FirstOrDefault()?.Id;
        var insertedEntities = allEntitiesFromApi.Where(x => x.Id != newestId).ToList();
        var insertedRowsCount = 0;
        foreach (var entity in insertedEntities)
        {
            insertedRowsCount += await dbService.Insert(entity, Consts.TABLE_NAME_TODO_STATUS);
        }

        var actual = await worker.AddNewTodoStatusesToDb();
        var expected = 1;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddNewTodoCategoryToDbTest()
    {
        // подготовка БД
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_TODO_CATEGORY);
        var allEntitiesFromApi = await apiService.GetAndMapEntitiesWithoutPayload<TodoCategory>(Consts.ENTITY_NAME_TODO_CATEGORY);
        Assert.That(allEntitiesFromApi, Is.Not.Empty);
        var newestId = allEntitiesFromApi.OrderByDescending(x => long.Parse(x.Id)).FirstOrDefault()?.Id;
        var insertedEntities = allEntitiesFromApi.Where(x => x.Id != newestId).ToList();
        var insertedRowsCount = 0;
        foreach (var entity in insertedEntities)
        {
            insertedRowsCount += await dbService.Insert(entity, Consts.TABLE_NAME_TODO_CATEGORY);
        }

        var actual = await worker.AddNewTodoCategoriesToDb();
        var expected = 1;

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetAndMapDepartmentsTest()
    {
        // подготовка БД
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEPARTMENT);
        var allEntitiesFromApi = await apiService.GetAndMapEntitiesWithoutPayload<Department>(Consts.ENTITY_NAME_DEPARTMENT);
        Assert.That(allEntitiesFromApi, Is.Not.Empty);
        var insertedRowsCount = 0;
        foreach (var entity in allEntitiesFromApi)
        {
            insertedRowsCount += await dbService.Insert(entity, Consts.TABLE_NAME_DEPARTMENT);
        }

        var entitiesFromDb = await dbService.GetAndMapEnteties<Department>(Consts.TABLE_NAME_DEPARTMENT);
        Assert.That(entitiesFromDb, Is.Not.Empty);
    }

    [Test]
    public async Task CheckCorrectQueryAndParametersTest()
    {
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Consts.TEST_FOLDER_NAME}\\InsertDealTest.json");
        if (!File.Exists(jsonFile)) throw new FileNotFoundException(jsonFile);
        var deaserializedDeals = JsonHelper.LoadDealsFromFile(jsonFile);
        Deal deaserializedDeal = null;

        foreach (var deal in deaserializedDeals)
        {
            await dbService.Insert(deal, Consts.TABLE_NAME_DEAL);
            deaserializedDeal = deal;

        }
        var mappedDeal = await dbService.GetAndMapDeal(long.Parse(deaserializedDeal.Id));
        var diffs = Logic.GetDifferences(deaserializedDeal, mappedDeal);
        var actual = diffs.Count == 0;
        var expected = true;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetDealStatusHystoryWithDealIdTest()
    {
        // создание ситуации, когда в CRM есть dealsCount - 1 сделок с id больше, чем в БД
        var jsonPayload = DealPayload.GetLastEntitiesPayload(entitiesCount);
        var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
        var deals = await apiService.GetAndMapEntities<Deal>(Consts.ENTITY_NAME_DEAL,
            jsonPayload, normalizer);
        if (deals.Count == 0) { throw new Exception(); }
        var oldestDealId = Logic.GetOldestId(deals);
        var oldestDeal = deals.Where(deal => deal.Id == oldestDealId.ToString()).FirstOrDefault();
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        var rowsInserted = await dbService.Insert(oldestDeal, Consts.TABLE_NAME_DEAL);

        var actual = await worker.AddNewDealsToDb();
        var expected = entitiesCount - 1;
        Assert.That(actual, Is.EqualTo(expected));
    }

    // переписать тест, изминилась логика обновления сделок
    [Test]
    [Ignore("Тест временно отключен, так как изменилась логика метода UpdateDealsInDb")]
    public async Task UpdateDealsInDbTest()
    {
        var rowsDeleted = await dbService.DeleteAllEntries(Consts.TABLE_NAME_DEAL);
        var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Consts.TEST_FOLDER_NAME}\\UpdateDealsInDbTest.json");
        if (!File.Exists(jsonFile)) throw new FileNotFoundException(jsonFile);
        var dealsFromFile = JsonHelper.LoadDealsFromFile(jsonFile);
        foreach (var deal in dealsFromFile)
        {
            var rowsInserted = await dbService.Insert(deal, Consts.TABLE_NAME_DEAL);
        }

        var actual = await worker.UpdateDealsInDb();
        var expected = 3;
        Assert.That(actual, Is.EqualTo(expected));
    }

    // debugTests

    // maintenance 1. Долго работает. Запускать время от времени (раз в неделю?), в идеале,
    // не должно давать результаты, иначе корректируй логику обновления сделок
    // !!! Работа с боевой БД
    [Test]
    [Explicit("Обслуживание. Сверка и корректировка сделок: активные бд - апи (независимо от даты обновления сделки)")]
    public async Task CollationActiveDealsDb_ApiTest()
    {
        Logger.Initialize();
        ILogger logger = Logger.Instance;
        logger.LogInformation("Сверка и корректировка сделок: активные бд - апи");

        var dataAccessOriginal = new MySqlDataAccess(logger, Consts.CONNECTION_STRING_REMOTE);
        var dbDataMapperOriginal = new DbDataMapper(logger);
        DbService dbServiceOriginal = new(logger, dataAccessOriginal, dbDataMapperOriginal);
        var activeDealsFromDb = await dbServiceOriginal.GetAndMapActiveDeals();

        var activeInDbButNotActiveInApi = new List<Deal>();
        var counter = 0;
        File.AppendAllText(dealsNotExistsInMegaplanFile, headers + "\n");
        foreach (var dbDeal in activeDealsFromDb)
        {
            var apiDeal = await apiService.GetAndMapDeal(long.Parse(dbDeal.Id));
            if (apiDeal == null)
            {
                File.AppendAllText(dealsNotExistsInMegaplanFile, $"{dbDeal.Id};{dbDeal.Number}" + "\n");
                continue;
            }
            var changedFields = Logic.GetDifferences(dbDeal, apiDeal);
            if (changedFields.Count > 0)
            {
                counter += await dbServiceOriginal.UpdateEntity(long.Parse(dbDeal.Id), Consts.TABLE_NAME_DEAL, changedFields);
            }
        }
        dataAccessOriginal.Dispose();
        Assert.Pass();
    }

    // maintenance 2. Запускать после CollationActiveDealsDb_ApiTest
    // !!! Работа с боевой БД
    [Test]
    [Explicit("Обслуживание. Удаление сделок из бд, которых нет в CRM")]
    public async Task DeleteDealsFromDbTest()
    {
        Logger.Initialize();
        ILogger logger = Logger.Instance;
        logger.LogInformation("Удаляю сделки из базы данных, которых нет в мегаплане (были удалены)");

        var dataAccessOriginal = new MySqlDataAccess(logger, Consts.CONNECTION_STRING_REMOTE);
        var dbDataMapperOriginal = new DbDataMapper(logger);
        DbService dbServiceOriginal = new(logger, dataAccessOriginal, dbDataMapperOriginal);
        var fileArr = File.ReadAllLines(dealsNotExistsInMegaplanFile);
        var notExistInApiDealsIds = fileArr
               .Where(l => !l.Contains(headers) && !string.IsNullOrWhiteSpace(l))
               .Select(l => long.Parse(l.Split(";")[0]));
        var counter = 0;
        foreach (var id in notExistInApiDealsIds)
        {
            counter += await dbServiceOriginal.DeleteEntity(id, Consts.TABLE_NAME_DEAL);
        }
        logger.LogInformation($"Удалено: {counter}");
        dataAccessOriginal.Dispose();
        Assert.Pass();
    }

    // !!! Работа с боевой БД
    [Test]
    [Explicit("Отладка")]
    public async Task UpdateDealsAndStatusesInDbDebugTest()
    {
        Logger.Initialize();
        ILogger logger = Logger.Instance;
        logger.LogInformation("Отладка");

        var dataAccessOriginal = new MySqlDataAccess(logger, Consts.CONNECTION_STRING_REMOTE);
        var dbDataMapperOriginal = new DbDataMapper(logger);
        DbService dbServiceOriginal = new(logger, dataAccessOriginal, dbDataMapperOriginal);
        var apiClient = new MegaApiClient(logger: logger, tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
        tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN, baseApiUrl: appSettings.BaseApUrl, username: appSettings.Username, password: appSettings.Password);
        var apiDataMapper = new ApiDataMapper(logger);
        ApiService apiService = new(logger, apiClient, apiDataMapper);
        var worker = new Worker(apiService, dbServiceOriginal, logger);

        var date = "2025-09-25 08:00:00";
        var lastUpdateDateTime = Logic.ParseLogDateTime(date);
        await worker.UpdateDealsInDb(lastUpdateDateTime);
        await worker.AddNewDealsStatusesHistoryToDb();

        dataAccessOriginal.Dispose();
        Assert.Pass();
    }

    [Test]
    [Explicit("Обслуживание. Массовое обновление сделок в CRM")]
    public async Task BulkUpdateTest()
    {
        Logger.Initialize();
        ILogger logger = Logger.Instance;

        logger.LogInformation("Отладка");

        var apiClient = new MegaApiClient(logger: logger, tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
        tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN, baseApiUrl: appSettings.BaseApUrl, username: appSettings.Username, password: appSettings.Password);
        var apiDataMapper = new ApiDataMapper(logger);
        ApiService apiService = new(logger, apiClient, apiDataMapper);
        var worker = new Worker(apiService, dbService, logger);

        // получение списка сделок в рамках фильтра
        //string filterId = "7/4376";
        //var payload = DealPayload.GetPayload(filterId: filterId);
        //var filtredDeals = await apiService.GetAndMapAllEntities<Deal>(Consts.ENTITY_NAME_DEAL, payload, filterId: filterId);
        //var ids = filtredDeals.Select(x => x.Id).ToList();
        //foreach(var id in ids)
        //{
        //    File.AppendAllText("roistat.txt", id + "\n");
        //}

        Dictionary<string, object> fieldsToUpdate = new()
        {
            { "Category1000051CustomFieldRoistat", "direct1" }
        };

        var dealIdsToUpdate = File.ReadLines("roistat.txt").Select(x => long.Parse(x));
        await worker.BulkUpdateDealsInCrm(dealIdsToUpdate, fieldsToUpdate);

        Assert.Pass();
    }

    [Test]
    [Explicit("Обслуживание: Обновление сделок в удаленной БД")]
    public async Task UpdateDealsInDb_Test()
    {
        Logger.Initialize();
        ILogger logger = Logger.Instance;
        logger.LogInformation("Обновление сделок в режиме обслуживания");

        var dataAccessOriginal = new MySqlDataAccess(logger, Consts.CONNECTION_STRING_REMOTE);
        var dbDataMapperOriginal = new DbDataMapper(logger);
        DbService dbServiceOriginal = new(logger, dataAccessOriginal, dbDataMapperOriginal);
        var apiClient = new MegaApiClient(logger: logger, tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
        tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN, baseApiUrl: appSettings.BaseApUrl, username: appSettings.Username, password: appSettings.Password);
        var apiDataMapper = new ApiDataMapper(logger);
        ApiService apiService = new(logger, apiClient, apiDataMapper);
        var worker = new Worker(apiService, dbServiceOriginal, logger);

        // Изменить дату, с которой просматривать сделки в api
        var startDateTime = new DateTime(2023, 11, 14);
        await worker.UpdateDealsInDb(startDateTime);

        dataAccessOriginal.Dispose();
        Assert.Pass();
    }

    [TearDown]
    public void TearDown()
    {
        dataAccess.Dispose();
    }
  // 36414
    static string _isertTestDealQuery =
@"
insert into `deals` (`id`, `number`, `name`, `shortDescription`, `contractor`, `manager`, `price`, `state`, `result`, `timeCreated`, `timeUpdated`, `Category1000051CustomFieldOplata1`, `Category1000051CustomFieldDataOplati1`, `Category1000051CustomFieldFakticheskayaDataOplati1`, `Category1000051CustomFieldOplata2`, `Category1000051CustomFieldDataOplati2`, `Category1000051CustomFieldFakticheskayaDataOplati2`, `Category1000051CustomFieldDataOplati21`, `Category1000051CustomFieldDataOplati3`, `Category1000051CustomFieldFakticheskayaDataOplati3`, `Category1000051CustomFieldDataVipolneniyaKommUsl`, `Category1000051CustomFieldSmetnayaPribilSUchetomNaloga`, `Category1000051CustomFieldTrudozatratiChS`, `Category1000051CustomFieldOplata4`, `Category1000051CustomFieldPlaniruemayaDataOplati4`, `Category1000051CustomFieldFakticheskayaDataOplati31`, `Category1000051CustomFieldPoluchenaOplata1`, `Category1000051CustomFieldPoluchenaOplata2`, `Category1000051CustomFieldPoluchenaOplata3`, `Category1000051CustomFieldPoluchenaOplata4`, `Category1000051CustomFieldPlaniruemayaSummaOplati`, `Category1000051CustomFieldVeroyatnostUspeha`, `Category1000051CustomFieldKommercheskoePredlozhenie`, `Category1000051CustomFieldTipZayavki`, `Category1000051CustomFieldRentabelnost`, `Category1000051CustomFieldDataGotovnostiOborudovaniya`, `Category1000051CustomFieldOtzivPismoPoluchen`, `Category1000051CustomFieldDataPolucheniyaOtzivaPisma`, `Category1000051CustomFieldVideootzivPoluchen`, `Category1000051CustomFieldDataPolucheniyaVideotziva`, `Category1000051CustomFieldKeysProektaOformlen`, `Category1000051CustomFieldDataOformleniyaKeysa`, `Category1000051CustomFieldZakrivayushchieDokumentiPeredaniVBuh`, `Category1000051CustomFieldDataPeredachiZakrivayushchihDokument`) values('36414','1488','Руководитель направления \""Дистрибуция\""','Сделка 1488 Руководитель направления \""Дистрибуция\"" с Моисеев Сергей','{\""contentType\"":\""ContractorHuman\"",\""id\"":\""1053710\"",\""humanNumber\"":53710,\""type\"":{\""contentType\"":\""ContractorType\"",\""id\"":\""1000007\""},\""responsibles\"":[{\""contentType\"":\""Employee\"",\""id\"":\""1000265\""}],\""responsiblesCount\"":1,\""canSeeFull\"":true,\""possibleActions\"":[\""act_edit\"",\""message\"",\""task\"",\""add_item\"",\""add_deal\"",\""act_fake_drop\"",\""act_invite\"",\""act_read\"",\""act_attaches\"",\""read_full\""],\""contactInfo\"":[{\""contentType\"":\""ContactInfo\"",\""type\"":\""email\"",\""value\"":\""serzh_moiseev_86@mail.ru\"",\""comment\"":null,\""isMain\"":true,\""subject\"":{\""contentType\"":\""ContractorHuman\"",\""id\"":\""1053710\""}},{\""contentType\"":\""ContactInfo\"",\""type\"":\""phone\"",\""value\"":\""+7 951 466-49-22\"",\""comment\"":\""\"",\""isMain\"":false}],\""contactInfoCount\"":2,\""description\"":\""Кандидат создан путем автоматической интеграции рекрутинговой платформы headhunter и Мегаплана 04.09.2025 в 17:34\"",\""textDescription\"":\""Кандидат создан путем автоматической интеграции рекрутинговой платформы headhunter и Мегаплана 04.09.2025 в 17:34\"",\""birthday\"":null,\""preferTransport\"":\""\"",\""status\"":null,\""isDropped\"":false,\""firstName\"":\""Сергей\"",\""middleName\"":\""\"",\""lastName\"":\""Моисеев\"",\""dateLastReadNews\"":null,\""company\"":null,\""gender\"":\""male\"",\""position\"":null,\""loginEmail\"":null,\""lastOnline\"":null,\""canLogin\"":false,\""isGuestAccessEnabled\"":false,\""avatar\"":null,\""isFavorite\"":false,\""tags\"":[],\""tagsCount\"":0,\""commentsCount\"":0,\""unreadCommentsCount\"":0,\""subscribed\"":false}','{\""contentType\"":\""Employee\"",\""id\"":\""1000265\""}','{\""contentType\"":\""Money\"",\""currency\"":\""RUB\"",\""valueInMain\"":0,\""rate\"":1,\""value\"":0}','{\""contentType\"":\""ProgramState\"",\""id\"":\""402\""}','active','{\""contentType\"":\""DateTime\"",\""value\"":\""2025-09-04T14:34:46+00:00\""}','{\""contentType\"":\""DateTime\"",\""value\"":\""2025-09-10T04:46:37+00:00\""}','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0');
";
}
