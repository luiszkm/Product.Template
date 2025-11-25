using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Neuraptor.ERP.Api.Controllers.v1;

/// <summary>
/// Products API - Version 1.0
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retorna todos os produtos (v1)
    /// </summary>
    /// <returns>Lista de produtos</returns>
    /// <response code="200">Lista de produtos retornada com sucesso</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista todos os produtos",
        Description = "Retorna uma lista paginada de produtos disponíveis no catálogo (versão 1.0)",
        OperationId = "GetProducts_V1"
    )]
    [SwaggerResponse(200, "Success", typeof(IEnumerable<ProductDto>))]
    public ActionResult<IEnumerable<ProductDto>> GetAll()
    {
        _logger.LogInformation("API v1.0 - Listando todos os produtos");

        var products = new[]
        {
            new ProductDto { Id = 1, Name = "Product A", Price = 19.99m },
            new ProductDto { Id = 2, Name = "Product B", Price = 29.99m },
            new ProductDto { Id = 3, Name = "Product C", Price = 39.99m }
        };

        return Ok(products);
    }

    /// <summary>
    /// Retorna um produto por ID (v1)
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <returns>Produto encontrado</returns>
    /// <response code="200">Produto encontrado</response>
    /// <response code="404">Produto não encontrado</response>
    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Busca produto por ID",
        Description = "Retorna os detalhes de um produto específico pelo seu identificador (versão 1.0)",
        OperationId = "GetProductById_V1"
    )]
    [SwaggerResponse(200, "Success", typeof(ProductDto))]
    [SwaggerResponse(404, "Not Found")]
    public ActionResult<ProductDto> GetById(int id)
    {
        _logger.LogInformation("API v1.0 - Buscando produto com ID: {ProductId}", id);

        if (id <= 0)
        {
            return NotFound(new { message = "Produto não encontrado" });
        }

        var product = new ProductDto
        {
            Id = id,
            Name = $"Product {id}",
            Price = id * 10.99m
        };

        return Ok(product);
    }

    /// <summary>
    /// Cria um novo produto (v1)
    /// </summary>
    /// <param name="product">Dados do produto</param>
    /// <returns>Produto criado</returns>
    /// <response code="201">Produto criado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cria um novo produto",
        Description = "Adiciona um novo produto ao catálogo (versão 1.0)",
        OperationId = "CreateProduct_V1"
    )]
    [SwaggerResponse(201, "Created", typeof(ProductDto))]
    [SwaggerResponse(400, "Bad Request")]
    public ActionResult<ProductDto> Create([FromBody] CreateProductDto product)
    {
        _logger.LogInformation("API v1.0 - Criando novo produto: {ProductName}", product.Name);

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return BadRequest(new { message = "Nome do produto é obrigatório" });
        }

        var createdProduct = new ProductDto
        {
            Id = Random.Shared.Next(1, 1000),
            Name = product.Name,
            Price = product.Price
        };

        return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
    }
}

/// <summary>
/// DTO de produto (v1)
/// </summary>
public class ProductDto
{
    /// <summary>
    /// ID do produto
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome do produto
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Preço do produto
    /// </summary>
    public decimal Price { get; set; }
}

/// <summary>
/// DTO para criação de produto (v1)
/// </summary>
public class CreateProductDto
{
    /// <summary>
    /// Nome do produto
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Preço do produto
    /// </summary>
    public decimal Price { get; set; }
}
