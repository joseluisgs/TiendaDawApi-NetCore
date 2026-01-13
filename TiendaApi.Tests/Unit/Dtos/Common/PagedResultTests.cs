using FluentAssertions;
using TiendaApi.Apis.Dtos.Common;

namespace TiendaApi.Tests.Unit.Dtos.Common;

/// <summary>
/// Tests unitarios para PagedResult{T}.
/// Verifica el funcionamiento correcto de la paginación de resultados.
/// </summary>
public class PagedResultTests
{
    #region Tests de TotalPages

    /// <summary>
    /// Verifica que con 25 elementos y 10 por página se obtienen 3 páginas.
    /// </summary>
    [Test]
    public void TotalPages_Con25ElementosY10PorPagina_Retorna3()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 25,
            Page = 1,
            PageSize = 10
        };

        // Assert
        resultado.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// Verifica que con 20 elementos y 10 por página se obtienen 2 páginas.
    /// </summary>
    [Test]
    public void TotalPages_Con20ElementosY10PorPagina_Retorna2()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 20,
            Page = 1,
            PageSize = 10
        };

        // Assert
        resultado.TotalPages.Should().Be(2);
    }

    /// <summary>
    /// Verifica que con 21 elementos y 10 por página se obtienen 3 páginas.
    /// </summary>
    [Test]
    public void TotalPages_Con21ElementosY10PorPagina_Retorna3()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 21,
            Page = 1,
            PageSize = 10
        };

        // Assert
        resultado.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// Verifica que con 0 elementos se obtienen 0 páginas.
    /// </summary>
    [Test]
    public void TotalPages_Con0Elementos_Retorna0()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        // Assert
        resultado.TotalPages.Should().Be(0);
    }

    /// <summary>
    /// Verifica que con tamaño de página 0 se obtienen 0 páginas.
    /// </summary>
    [Test]
    public void TotalPages_ConPageSize0_Retorna0()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 25,
            Page = 1,
            PageSize = 0
        };

        // Assert
        resultado.TotalPages.Should().Be(0);
    }

    /// <summary>
    /// Verifica que con elementos exactos se calcula correctamente.
    /// </summary>
    [Test]
    public void TotalPages_ConElementoExacto_DivisionExacta()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 100,
            Page = 1,
            PageSize = 10
        };

        // Assert
        resultado.TotalPages.Should().Be(10);
    }

    #endregion

    #region Tests de HasNextPage

    /// <summary>
    /// Verifica que en la primera página con más páginas hay página siguiente.
    /// </summary>
    [Test]
    public void HasNextPage_EnPrimeraPaginaConMasPaginas_RetornaTrue()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 25,
            Page = 1,
            PageSize = 10
        };

        // Assert
        resultado.HasNextPage.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que en la última página no hay página siguiente.
    /// </summary>
    [Test]
    public void HasNextPage_EnUltimaPagina_RetornaFalse()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 25,
            Page = 3,
            PageSize = 10
        };

        // Assert
        resultado.HasNextPage.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que en una página intermedia hay página siguiente.
    /// </summary>
    [Test]
    public void HasNextPage_EnPaginaIntermedia_RetornaTrue()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 50,
            Page = 2,
            PageSize = 10
        };

        // Assert
        resultado.HasNextPage.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que sin elementos no hay página siguiente.
    /// </summary>
    [Test]
    public void HasNextPage_SinElementos_RetornaFalse()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        // Assert
        resultado.HasNextPage.Should().BeFalse();
    }

    #endregion

    #region Tests de HasPreviousPage

    /// <summary>
    /// Verifica que en la primera página no hay página anterior.
    /// </summary>
    [Test]
    public void HasPreviousPage_EnPrimeraPagina_RetornaFalse()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 25,
            Page = 1,
            PageSize = 10
        };

        // Assert
        resultado.HasPreviousPage.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que en la segunda página hay página anterior.
    /// </summary>
    [Test]
    public void HasPreviousPage_EnSegundaPagina_RetornaTrue()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 25,
            Page = 2,
            PageSize = 10
        };

        // Assert
        resultado.HasPreviousPage.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que en la última página hay página anterior.
    /// </summary>
    [Test]
    public void HasPreviousPage_EnUltimaPagina_RetornaTrue()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 25,
            Page = 3,
            PageSize = 10
        };

        // Assert
        resultado.HasPreviousPage.Should().BeTrue();
    }

    #endregion

    #region Tests de Inicialización de Items

    /// <summary>
    /// Verifica que por defecto Items es una colección vacía.
    /// </summary>
    [Test]
    public void Items_PorDefecto_RetornaColeccionVacia()
    {
        // Arrange & Act
        var resultado = new PagedResult<string>();

        // Assert
        resultado.Items.Should().NotBeNull();
        resultado.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que se pueden asignar elementos a Items.
    /// </summary>
    [Test]
    public void Items_ConElementos_RetornaElementos()
    {
        // Arrange
        var elementos = new List<string> { "elemento1", "elemento2", "elemento3" };

        // Act
        var resultado = new PagedResult<string>
        {
            Items = elementos,
            TotalCount = 3,
            Page = 1,
            PageSize = 10
        };

        // Assert
        resultado.Items.Should().HaveCount(3);
        resultado.Items.Should().Contain("elemento1");
        resultado.Items.Should().Contain("elemento2");
        resultado.Items.Should().Contain("elemento3");
    }

    #endregion

    #region Tests de Propiedades

    /// <summary>
    /// Verifica que todas las propiedades se asignan correctamente.
    /// </summary>
    [Test]
    public void PagedResult_AsignaTodosLosCampos_Correctamente()
    {
        // Arrange
        var elementos = new List<int> { 1, 2, 3 };

        // Act
        var resultado = new PagedResult<int>
        {
            Items = elementos,
            TotalCount = 100,
            Page = 5,
            PageSize = 20
        };

        // Assert
        resultado.TotalCount.Should().Be(100);
        resultado.Page.Should().Be(5);
        resultado.PageSize.Should().Be(20);
        resultado.TotalPages.Should().Be(5);
        resultado.HasNextPage.Should().BeFalse();
        resultado.HasPreviousPage.Should().BeTrue();
    }

    #endregion

    #region Tests de Casos Límite

    /// <summary>
    /// Verifica el cálculo con un elemento y tamaño de página grande.
    /// </summary>
    [Test]
    public void TotalPages_ConUnElementoYPageSizeGrande_Retorna1()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 1,
            Page = 1,
            PageSize = 100
        };

        // Assert
        resultado.TotalPages.Should().Be(1);
    }

    /// <summary>
    /// Verifica el cálculo cuando la página excede el total de páginas.
    /// </summary>
    [Test]
    public void TotalPages_ConPaginaMayorATotalPages_RetornaPaginaCorrecta()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 25,
            Page = 5,
            PageSize = 10
        };

        // Assert
        resultado.TotalPages.Should().Be(3);
        resultado.HasNextPage.Should().BeFalse();
    }

    /// <summary>
    /// Verifica HasNextPage cuando los elementos son justos en la última página.
    /// </summary>
    [Test]
    public void HasNextPage_ConElementosJustosEnUltimaPagina_RetornaFalse()
    {
        // Arrange
        var resultado = new PagedResult<string>
        {
            TotalCount = 30,
            Page = 3,
            PageSize = 10
        };

        // Assert
        resultado.HasNextPage.Should().BeFalse();
    }

    #endregion
}
