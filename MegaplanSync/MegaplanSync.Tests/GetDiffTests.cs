using MegaplanSync.Core.Models;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core;
using System.Text.Json;

namespace MegaplanSync.Tests;

[TestFixture]
public class GetDiffTests
{
    private Deal CreateOriginalDeal()
    {
        return new Deal
        {
            Id = "1",
            Name = "Original Deal",
            Price = new Money { Value = 100.0m },
            State = new ProgramState { Id = "101", Name = "Active" },
            PoluchenaOplata1 = false,
            TrudozatratiChS = 10.5,
            KommercheskoePredlozhenie = new List<FileObject>()
        };
    }

    private Deal CreateUpdatedDeal()
    {
        return new Deal
        {
            Id = "1",
            Name = "Updated Deal",
            Price = new Money { Value = 150.0m },
            State = new ProgramState { Id = "102", Name = "Closed" },
            PoluchenaOplata1 = true,
            TrudozatratiChS = 10.5,
            KommercheskoePredlozhenie = new List<FileObject>()
        };
    }

    [Test]
    public void GetDifferences_NameChanged_ReturnsDifference()
    {
        // Arrange
        var original = CreateOriginalDeal();
        var updated = CreateUpdatedDeal();

        // Act
        var differences = Logic.GetDifferences(original, updated);

        // Assert
        Assert.That(differences, Has.Count.EqualTo(4));
        Assert.That(differences.ContainsKey("name"), Is.True);
        Assert.That(differences["name"], Is.EqualTo("Updated Deal"));
    }

    [Test]
    public void GetDifferences_ComplexObjectChanged_ReturnsSerializedString()
    {
        // Arrange
        var original = CreateOriginalDeal();
        var updated = CreateUpdatedDeal();
        var expectedPriceJson = JsonSerializer.Serialize(updated.Price);
        var expectedStateJson = JsonSerializer.Serialize(updated.State);

        // Act
        var differences = Logic.GetDifferences(original, updated);

        // Assert
        Assert.That(differences.ContainsKey("price"), Is.True);
        Assert.That(differences.ContainsKey("state"), Is.True);
        Assert.That(differences["price"], Is.EqualTo(expectedPriceJson));
        Assert.That(differences["state"], Is.EqualTo(expectedStateJson));
    }

    [Test]
    public void GetDifferences_BoolChanged_ReturnsBooleanValue()
    {
        // Arrange
        var original = CreateOriginalDeal();
        var updated = CreateUpdatedDeal();

        // Act
        var differences = Logic.GetDifferences(original, updated);

        // Assert
        Assert.That(differences.ContainsKey("Category1000051CustomFieldPoluchenaOplata1"), Is.True);
        Assert.That(differences["Category1000051CustomFieldPoluchenaOplata1"], Is.EqualTo(true));
    }

    [Test]
    public void GetDifferences_NoChanges_ReturnsEmptyDictionary()
    {
        // Arrange
        var original = CreateOriginalDeal();
        var updated = CreateOriginalDeal(); // Сравниваем идентичные объекты

        // Act
        var differences = Logic.GetDifferences(original, updated);

        // Assert
        Assert.That(differences, Is.Empty);
    }

    [Test]
    public void GetDifferences_NullUpdatedObject_ReturnsNullDifference()
    {
        // Arrange
        var original = CreateOriginalDeal();
        var updated = new Deal { Price = null }; // Отличается только одно поле

        // Act
        var differences = Logic.GetDifferences(original, updated);

        // Assert
        Assert.That(differences.ContainsKey("price"), Is.True);
        Assert.That(differences["price"], Is.EqualTo("0"));
    }

    [Test]
    public void GetDifferences_OriginalIsNull_ReturnsDifference()
    {
        // Arrange
        var original = new Deal { Price = null };
        var updated = CreateUpdatedDeal();

        // Act
        var differences = Logic.GetDifferences(original, updated);

        // Assert
        Assert.That(differences.ContainsKey("price"), Is.True);
        Assert.That(differences["price"], Is.Not.Null);
    }

    [Test]
    public void GetDifferences_ListChanged_NoDifferencesReturned()
    {
        // Arrange
        var original = CreateOriginalDeal();
        var updated = CreateUpdatedDeal();
        updated.KommercheskoePredlozhenie.Add(new FileObject());

        // Act
        var differences = Logic.GetDifferences(original, updated);

        // Assert
        Assert.That(differences.ContainsKey("KommercheskoePredlozhenie"), Is.False);
    }
}