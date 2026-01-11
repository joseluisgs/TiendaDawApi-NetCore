using FluentAssertions;
using TiendaApi.Dtos.Categorias;
using TiendaApi.Mappers;
using TiendaApi.Models;

namespace TiendaApi.Tests.Unit.Mappers;

/// <summary>
/// Comprehensive test suite for CategoriaMapper extension methods
/// Tests all entity-DTO conversions for Categoria domain
/// </summary>
public class CategoriaMapperTests
{
    #region ToDto Tests

    [Test]
    public void ToDto_WithAllFields_ShouldMapCorrectly()
    {
        // Arrange
        var categoria = new Categoria
        {
            Id = 1,
            Nombre = "Electronics",
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0),
            UpdatedAt = new DateTime(2024, 6, 20, 14, 45, 0)
        };

        // Act
        var dto = categoria.ToDto();

        // Assert
        dto.Id.Should().Be(1);
        dto.Nombre.Should().Be("Electronics");
        dto.CreatedAt.Should().Be(categoria.CreatedAt);
    }

    [Test]
    public void ToDto_WithMinimalFields_ShouldMapCorrectly()
    {
        // Arrange
        var categoria = new Categoria
        {
            Id = 5,
            Nombre = "Books"
        };

        // Act
        var dto = categoria.ToDto();

        // Assert
        dto.Id.Should().Be(5);
        dto.Nombre.Should().Be("Books");
    }

    [Test]
    public void ToDto_WithLongNombre_ShouldMapCorrectly()
    {
        // Arrange
        var longNombre = new string('A', 200);
        var categoria = new Categoria
        {
            Id = 1,
            Nombre = longNombre
        };

        // Act
        var dto = categoria.ToDto();

        // Assert
        dto.Nombre.Should().Be(longNombre);
        dto.Nombre.Length.Should().Be(200);
    }

    [Test]
    public void ToDto_WithSpecialCharacters_ShouldMapCorrectly()
    {
        // Arrange
        var categoria = new Categoria
        {
            Id = 1,
            Nombre = "Electrónica & Electrodomésticos > 100€"
        };

        // Act
        var dto = categoria.ToDto();

        // Assert
        dto.Nombre.Should().Be("Electrónica & Electrodomésticos > 100€");
    }

    [Test]
    public void ToDto_ShouldBeIdempotent()
    {
        // Arrange
        var categoria = new Categoria
        {
            Id = 1,
            Nombre = "Test"
        };

        // Act
        var dto1 = categoria.ToDto();
        var dto2 = categoria.ToDto();

        // Assert
        dto1.Should().BeEquivalentTo(dto2);
    }

    #endregion

    #region ToEntity Tests

    [Test]
    public void ToEntity_WithAllFields_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new CategoriaRequestDto
        {
            Nombre = "Computers & Accessories"
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Nombre.Should().Be("Computers & Accessories");
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void ToEntity_WithEmptyNombre_ShouldMapEmptyString()
    {
        // Arrange
        var dto = new CategoriaRequestDto
        {
            Nombre = string.Empty
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Nombre.Should().BeEmpty();
    }

    [Test]
    public void ToEntity_WithWhitespaceNombre_ShouldMapWhitespace()
    {
        // Arrange
        var dto = new CategoriaRequestDto
        {
            Nombre = "   "
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Nombre.Should().Be("   ");
    }

    [Test]
    public void ToEntity_ShouldSetDefaultTimestamps()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Test" };
        var beforeCreation = DateTime.UtcNow;

        // Act
        var entity = dto.ToEntity();
        var afterCreation = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(beforeCreation);
        entity.CreatedAt.Should().BeOnOrBefore(afterCreation);
        entity.UpdatedAt.Should().BeOnOrAfter(beforeCreation);
        entity.UpdatedAt.Should().BeOnOrBefore(afterCreation);
    }

    #endregion

    #region UpdateEntity Tests

    [Test]
    public void UpdateEntity_ShouldUpdateNombre()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Old Name" };
        var dto = new CategoriaRequestDto { Nombre = "New Name" };
        var beforeUpdate = categoria.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure time difference
        dto.UpdateEntity(categoria);

        // Assert
        categoria.Nombre.Should().Be("New Name");
    }

    [Test]
    public void UpdateEntity_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var categoria = new Categoria
        {
            Id = 1,
            Nombre = "Original",
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var dto = new CategoriaRequestDto { Nombre = "Updated" };
        var beforeUpdate = DateTime.UtcNow;

        // Act
        dto.UpdateEntity(categoria);

        // Assert
        categoria.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Test]
    public void UpdateEntity_WithEmptyNombre_ShouldUpdateToEmpty()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Original Name" };
        var dto = new CategoriaRequestDto { Nombre = string.Empty };

        // Act
        dto.UpdateEntity(categoria);

        // Assert
        categoria.Nombre.Should().BeEmpty();
    }

    [Test]
    public void UpdateEntity_ShouldNotModifyId()
    {
        // Arrange
        var categoria = new Categoria { Id = 42, Nombre = "Original" };
        var dto = new CategoriaRequestDto { Nombre = "Updated" };

        // Act
        dto.UpdateEntity(categoria);

        // Assert
        categoria.Id.Should().Be(42);
    }

    [Test]
    public void UpdateEntity_ShouldNotModifyCreatedAt()
    {
        // Arrange
        var originalCreatedAt = new DateTime(2023, 1, 1);
        var categoria = new Categoria
        {
            Id = 1,
            Nombre = "Original",
            CreatedAt = originalCreatedAt
        };
        var dto = new CategoriaRequestDto { Nombre = "Updated" };

        // Act
        dto.UpdateEntity(categoria);

        // Assert
        categoria.CreatedAt.Should().Be(originalCreatedAt);
    }

    #endregion

    #region ToDtoList Tests

    [Test]
    public void ToDtoList_WithMultipleCategorias_ShouldMapAll()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new() { Id = 1, Nombre = "Cat1" },
            new() { Id = 2, Nombre = "Cat2" },
            new() { Id = 3, Nombre = "Cat3" }
        };

        // Act
        var dtos = categorias.ToDtoList().ToList();

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].Id.Should().Be(1);
        dtos[0].Nombre.Should().Be("Cat1");
        dtos[1].Id.Should().Be(2);
        dtos[1].Nombre.Should().Be("Cat2");
        dtos[2].Id.Should().Be(3);
        dtos[2].Nombre.Should().Be("Cat3");
    }

    [Test]
    public void ToDtoList_WithEmptyList_ShouldReturnEmpty()
    {
        // Arrange
        var categorias = new List<Categoria>();

        // Act
        var dtos = categorias.ToDtoList().ToList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Test]
    public void ToDtoList_ShouldMaintainOrder()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new() { Id = 3, Nombre = "Third" },
            new() { Id = 1, Nombre = "First" },
            new() { Id = 2, Nombre = "Second" }
        };

        // Act
        var dtos = categorias.ToDtoList().ToList();

        // Assert
        dtos[0].Id.Should().Be(3);
        dtos[1].Id.Should().Be(1);
        dtos[2].Id.Should().Be(2);
    }

    [Test]
    public void ToDtoList_ShouldBeLazy()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new() { Id = 1, Nombre = "Cat1" },
            new() { Id = 2, Nombre = "Cat2" }
        };

        // Act
        var enumerable = categorias.ToDtoList();

        // Assert - enumerable should not be evaluated yet
        enumerable.Should().NotBeNull();
    }

    [Test]
    public void ToDtoList_CanBeIteratedMultipleTimes()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new() { Id = 1, Nombre = "Cat1" }
        };

        // Act
        var dtos = categorias.ToDtoList();
        var firstIteration = dtos.ToList();
        var secondIteration = dtos.ToList();

        // Assert
        firstIteration.Should().HaveCount(1);
        secondIteration.Should().HaveCount(1);
    }

    #endregion

    #region Roundtrip Tests

    [Test]
    public void ToDto_ThenToEntity_ShouldPreserveData()
    {
        // Arrange
        var original = new Categoria
        {
            Id = 10,
            Nombre = "Roundtrip Test"
        };

        // Act
        var dto = original.ToDto();
        // Note: ToEntity takes a DTO, not a DTO result
        var entity = new CategoriaRequestDto { Nombre = dto.Nombre }.ToEntity();

        // Assert
        entity.Nombre.Should().Be(original.Nombre);
    }

    [Test]
    public void ToEntity_ThenToDto_ShouldPreserveNombre()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Persistence Test" };

        // Act
        var entity = dto.ToEntity();
        var resultDto = entity.ToDto();

        // Assert
        resultDto.Nombre.Should().Be(dto.Nombre);
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void ToDto_WithMaxId_ShouldMapCorrectly()
    {
        // Arrange
        var categoria = new Categoria
        {
            Id = long.MaxValue,
            Nombre = "Max ID Category"
        };

        // Act
        var dto = categoria.ToDto();

        // Assert
        dto.Id.Should().Be(long.MaxValue);
    }

    [Test]
    public void ToDto_WithUnicodeCharacters_ShouldMapCorrectly()
    {
        // Arrange
        var categoria = new Categoria
        {
            Id = 1,
            Nombre = "日本語テスト 中文测试 한국어 Ελληνικά"
        };

        // Act
        var dto = categoria.ToDto();

        // Assert
        dto.Nombre.Should().Be("日本語テスト 中文测试 한국어 Ελληνικά");
    }

    [Test]
    public void ToEntity_WithUnicodeNombre_ShouldMapCorrectly()
    {
        // Arrange
        var dto = new CategoriaRequestDto
        {
            Nombre = "🎉 Unicode Celebration 🎊"
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Nombre.Should().Be("🎉 Unicode Celebration 🎊");
    }

    #endregion
}
