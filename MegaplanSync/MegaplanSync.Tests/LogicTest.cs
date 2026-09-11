using MegaplanSync.Core;
using MegaplanSync.Core.Models.Deal;


namespace MegaplanSync.Tests;

public class LogicTest
{
    [TestCase("2025-09-26 1:05:28", 2025, 9, 26, 1, 5, 28, TestName = "Single Digit Hour")]
    [TestCase("2025-09-26 14:05:28", 2025, 9, 26, 14, 5, 28, TestName = "Double Digit Hour")]
    [TestCase("2024-01-01 0:00:00", 2024, 1, 1, 0, 0, 0, TestName = "Midnight")]
    [TestCase("2023-12-31 23:59:59", 2023, 12, 31, 23, 59, 59, TestName = "EndOfYear")]
    [TestCase("2025-10-02 8:00:00", 2025, 10, 02, 8, 0, 0, TestName = "8:00:00")]
    [TestCase("2025-10-02 08:00:00", 2025, 10, 02, 8, 0, 0, TestName = "08:00:00")]
    [TestCase("02.10.2025 10:00:18", 2025, 10, 02, 10, 0, 18, TestName = "02.10.2025 10:00:18")]
    public void ParseLogDateTime_ShouldParseValidStrings(
        string input,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        var expectedDate = new DateTime(year, month, day, hour, minute, second);

        var actualDate = Logic.ParseLogDateTime(input);

        Assert.That(actualDate, Is.EqualTo(expectedDate), $"Ожидалось: {expectedDate}, Получено: {actualDate} для ввода: {input}");
    }


    [TestCase("26/09/2025 14:05:28", TestName = "Incorrect Separator")]
    [TestCase("26.09.2025 25:05:28", TestName = "Invalid Hour")]
    [TestCase("", TestName = "Empty String")]
    [TestCase("   ", TestName = "Whitespace String")]
    public void ParseLogDateTime_ShouldReturnDefault_OnInvalidInput(string input)
    {
        var expectedDate = default(DateTime); // 01.01.0001 00:00:00

        var actualDate = Logic.ParseLogDateTime(input);

        Assert.That(actualDate, Is.EqualTo(expectedDate), $"Ожидалось default({expectedDate}), но получено {actualDate}");
    }

    [Test]
    public void ParseLogDateTime_ShouldReturnDefault_OnNull()
    {
        string input = null;
        var expectedDate = default(DateTime);

        var actualDate = Logic.ParseLogDateTime(input);

        Assert.That(actualDate, Is.EqualTo(expectedDate));
    }

    [Test]
    public async Task GetNewestIdTest()
    {
        var deals = new List<Deal>
        {
            new Deal{Id = "100"},
            new Deal{Id = "150"},
            new Deal{Id = "1150"},
        };
        var actual = Logic.GetNewestId(deals);
        var expected = 1150;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetDifferencesEqualsDealsTest()
    {
        var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Consts.TEST_FOLDER_NAME}\\GetDifferencesTest.json");
        if (!File.Exists(jsonFile)) throw new FileNotFoundException(jsonFile);
        var dealsFromFile = JsonHelper.LoadDealsFromFile(jsonFile);
        var original = dealsFromFile[0];
        var updated = dealsFromFile[1];
        var diffs = Logic.GetDifferences(original, updated);
        var actual = diffs.Count;
        var expected = 0;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetDifferencesOnePositionTest()
    {
        var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), $"{Consts.TEST_FOLDER_NAME}\\GetDifferencesTest.json");
        if (!File.Exists(jsonFile)) throw new FileNotFoundException(jsonFile);
        var dealsFromFile = JsonHelper.LoadDealsFromFile(jsonFile);
        var original = dealsFromFile[0];
        var updated = dealsFromFile[1];
        original.Oplata1 = null;
        var diffs = Logic.GetDifferences(original, updated);
        var actual = diffs.Count;
        var expected = 1;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(IsTimeDiffTestData))]
    public void IsTimeDiffTest(DateTime one, DateTime two, bool expected)
    {
        var actual = Logic.IsTimeDiff(one, two);
        Assert.That(actual, Is.EqualTo(expected));
    }
    private static IEnumerable<TestCaseData> IsTimeDiffTestData()
    {
        yield return new TestCaseData(new DateTime(2025, 9, 4, 13, 48, 58), new DateTime(2025, 9, 4, 13, 48, 58), false);
        yield return new TestCaseData(new DateTime(2025, 9, 4, 13, 48, 58), new DateTime(2025, 9, 4, 13, 48, 57), true);
    }
}
