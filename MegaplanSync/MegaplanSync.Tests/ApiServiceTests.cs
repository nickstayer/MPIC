using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Core.Models.Contractor;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.Employee;
using MegaplanSync.Core.Models.MegaplanCallInfo;
using MegaplanSync.Service;
using Moq;
using System.Dynamic;

namespace MegaplanSync.Tests
{
    public class ApiServiceTests
    {
        ILogger logger;
        IApiClient apiClient;
        IApiDataMapper apiDataMapper;
        ApiService apiService;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger>().Object;
            var appSettings = TestConfiguration.GetAppSettings();
            if (appSettings.LaunchTime.Length == 0
                || string.IsNullOrWhiteSpace(appSettings.Username)
                || string.IsNullOrWhiteSpace(appSettings.Password)
                || string.IsNullOrWhiteSpace(appSettings.BaseApUrl)
                || string.IsNullOrWhiteSpace(appSettings.ConnectionString))
            {
                throw new Exception("Ошибка: некорректные настройки.");
            }
            apiDataMapper = new ApiDataMapper(logger);
            apiClient = new MegaApiClient(logger: logger, tokenFile: Consts.TOKEN_FILE_MEGAPLAN,
            tokenExpAtFile: Consts.TOKEN_EXP_AT_FILE_MEGAPLAN, baseApiUrl: appSettings.BaseApUrl, username: appSettings.Username, password: appSettings.Password);
            apiService = new ApiService(logger, apiClient, apiDataMapper);
        }

        // долгий тест
        //[Test]
        public async Task GetAndMapDealsUpdatedAfterTest()
        {
            var lastDateTimeUpdate = new DateTime(2025, 9, 11, 12, 0, 0);
            var lastUpdatedDeals = await apiService.GetAndMapDealsUpdatedAfter(lastDateTimeUpdate);
            var sorted = lastUpdatedDeals.OrderByDescending(d => d.TimeUpdated.Value).ToList();
            var oldest = sorted.Last().TimeUpdated.Value;
            var newest = sorted.First().TimeUpdated.Value;
            var actual = lastDateTimeUpdate < oldest
                && lastDateTimeUpdate < newest;
            var expected = true;
            Assert.That(actual, Is.EqualTo(expected));
        }

        // долгий тест
        [Test]
        public async Task GetAndMapActiveDealsAfterIdTest()
        {
            var activeDeals = await apiService.GetAndMapActiveDeals();
            var notActiveDeals = activeDeals.Where(d => d.Result != "active").ToList();
            var notActiveDeals2 = activeDeals.Where(d => d.State.Type != "active").ToList();
            var actual = notActiveDeals.Count == 0;
            var expected = true;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetAndMapLastDealTest()
        {
            var limit = 10;
            var jsonPayload = DealPayload.GetLastEntitiesPayload(limit);
            var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
            var lastDeals = await apiService.GetAndMapEntities<Deal>(Consts.ENTITY_NAME_DEAL, 
                jsonPayload, normalizer);
            var actual = lastDeals.Count == limit;
            var expected = true;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetAndMapLastCallInfosTest()
        {
            // TODO: найти последний id: 471987 !!!
            int limit = 100;
            var jsonPayload = MegaplanCallInfoPayload.GetPayload(471900);
            var lastEntries = await apiService.GetAndMapEntities<MegaplanCallInfo>(Consts.ENTITY_NAME_CALLINFO, jsonPayload);
            JsonHelper.SaveEntitiesToFile(lastEntries, "callIngos.json");
            Assert.Pass();
        }

        [Test]
        public async Task GetAndMapDealsAfterIdTest()
        {
            var limit = 1;
            var jsonPayload = DealPayload.GetLastEntitiesPayload(limit);
            var normalizer = new DataNormalizer(Consts.MAPPING_DEAL_RULES_FILE);
            var lastEntetyCollection = await apiService.GetAndMapEntities<Deal>(Consts.ENTITY_NAME_DEAL, 
                jsonPayload, normalizer);
            var lastEntity = lastEntetyCollection.FirstOrDefault();
            var entitiesCount = 4;
            long id = long.Parse(lastEntity?.Id) - entitiesCount;

            string jsonPayload2 = DealPayload.GetPayload(id, Consts.JSON_ENTRIES_LIMIT);
            var entitiesAfterId = await apiService.GetAndMapEntitiesAfterId<Deal>(Consts.ENTITY_NAME_DEAL, jsonPayload2, id, normalizer);

            Assert.That(entitiesAfterId, Is.Not.Empty);
            var testCollection = entitiesAfterId.Where(x => long.Parse(x.Id) <= id).ToList();
            var actual = testCollection.Count == 0;
            var expected = true;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetAndMapContractorsAfterIdTest()
        {
            var limit = 1;
            string jsonPayload = ContractorPayload.GetLastEntitiesPayload(limit);
            var lastEntetyCollection = await apiService.GetAndMapEntities<Contractor>(Consts.ENTITY_NAME_CONTRACTOR,
                jsonPayload);
            var lastEntity = lastEntetyCollection.FirstOrDefault();
            var entitiesCount = 4;
            long id = long.Parse(lastEntity?.Id) - entitiesCount;
            string jsonPayload2 = ContractorPayload.GetPayload(id, Consts.JSON_ENTRIES_LIMIT);
            var entitiesAfterId = await apiService.GetAndMapEntitiesAfterId<Contractor>(Consts.ENTITY_NAME_CONTRACTOR, jsonPayload2, id);
            Assert.That(entitiesAfterId, Is.Not.Empty);
            var testCollection = entitiesAfterId.Where(x => long.Parse(x.Id) <= id).ToList();
            var actual = testCollection.Count == 0;
            var expected = true;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetAndMapEmployeesAfterIdTest()
        {
            var limit = 1;
            string jsonPayload = EmployeePayload.GetLastEntitiesPayload(limit);
            var lastEntetyCollection = await apiService.GetAndMapEntities<Employee>(Consts.ENTITY_NAME_EMPLOYEE,
                jsonPayload);
            var lastEntity = lastEntetyCollection.FirstOrDefault();
            var entitiesCount = 4;
            long id = long.Parse(lastEntity?.Id) - entitiesCount;

            string jsonPayload2 = EmployeePayload.GetPayload(id, Consts.JSON_ENTRIES_LIMIT);
            var entitiesAfterId = await apiService.GetAndMapEntitiesAfterId<Employee>(Consts.ENTITY_NAME_EMPLOYEE, jsonPayload2,
                id);

            Assert.That(entitiesAfterId, Is.Not.Empty);
            var testCollection = entitiesAfterId.Where(x => long.Parse(x.Id) <= id).ToList();
            var actual = testCollection.Count == 0;
            var expected = true;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetAndMapContractorTest()
        {
            var id1 = 1000011;
            var id2 = 1000013;

            var contractorCompany = await apiService.GetAndMapContractor(id1);
            var contractorHuman = await apiService.GetAndMapContractor(id2);

            if (contractorCompany == null) throw new Exception();
            if (contractorHuman == null) throw new Exception();
            var actualCompany = contractorCompany.Name == "Коммунальные технологии";
            var actualHuman = contractorHuman.LastName == "Ребров" 
                && contractorHuman.FirstName == "Виктор" 
                && contractorHuman.MiddleName == "Владимирович";
            var actual = actualCompany && actualHuman;
            var expected = true;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TearDown]
        public void TearDown()
        {
            
        }
    }
}