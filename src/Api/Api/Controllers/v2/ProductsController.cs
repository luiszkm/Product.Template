using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Product.Template.Api.Configurations;
using Swashbuckle.AspNetCore.Annotations;

namespace Product.Template.Api.Controllers.v2;

/// <summary>
/// Products API - Version 2.0
/// </summary>
[ApiController]
[ApiVersion("2.0")]
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
    /// Retorna todos os produtos com informações adicionais (v2)
    /// </summary>
    /// <param name="page">Número da página</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <returns>Lista paginada de produtos</returns>
    /// <response code="200">Lista de produtos retornada com sucesso</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista todos os produtos com paginação",
        Description = "Retorna uma lista paginada de produtos com informações adicionais como categoria e estoque (versão 2.0)",
        OperationId = "GetProducts_V2"
    )]
    [SwaggerResponse(200, "Success", typeof(ProductListResponse))]
    public ActionResult<ProductListResponse> GetAll(
        [FromQuery, SwaggerParameter("Número da página (padrão: 1)")] int page = 1,
        [FromQuery, SwaggerParameter("Tamanho da página (padrão: 10)")] int pageSize = 10)
    {
        _logger.LogInformation("API v2.0 - Listando produtos - Página: {Page}, Tamanho: {PageSize}", page, pageSize);

        var products = new[]
        {
            new ProductDtoV2
            {
                Id = 1,
                Name = "Product A",
                Price = 19.99m,
                Category = "Electronics",
                Stock = 100,
                IsActive = true
            },
            new ProductDtoV2
            {
                Id = 2,
                Name = "Product B",
                Price = 29.99m,
                Category = "Books",
                Stock = 50,
                IsActive = true
            },
            new ProductDtoV2
            {
                Id = 3,
                Name = "Product C",
                Price = 39.99m,
                Category = "Clothing",
                Stock = 75,
                IsActive = true
            }
        };

        var response = new ProductListResponse
        {
            Items = products,
            TotalCount = products.Length,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(products.Length / (double)pageSize)
        };

        return Ok(response);
    }

    /// <summary>
    /// Retorna um produto por ID com informações adicionais (v2)
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <returns>Produto encontrado</returns>
    /// <response code="200">Produto encontrado</response>
    /// <response code="404">Produto não encontrado</response>
    [HttpGet("{id:int}")]
    [SwaggerOperation(
        Summary = "Busca produto por ID com detalhes completos",
        Description = "Retorna os detalhes completos de um produto incluindo categoria, estoque e status (versão 2.0)",
        OperationId = "GetProductById_V2"
    )]
    [SwaggerResponse(200, "Success", typeof(ProductDtoV2))]
    [SwaggerResponse(404, "Not Found")]
    public ActionResult<ProductDtoV2> GetById(int id)
    {
        // Exemplo de custom span/trace com OpenTelemetry
        using var activity = OpenTelemetryConfiguration.ActivitySource.StartActivity("GetProductById");
        activity?.SetTag("product.id", id);

        _logger.LogInformation("API v2.0 - Buscando produto com ID: {ProductId}", id);

        if (id <= 0)
        {
            activity?.SetTag("product.found", false);
            activity?.SetTag("error.reason", "invalid_id");
            return NotFound(new { message = "Produto não encontrado", errorCode = "PRODUCT_NOT_FOUND" });
        }

        // Simular uma operação de busca no banco de dados
        activity?.AddEvent(new System.Diagnostics.ActivityEvent("Querying database"));

        var product = new ProductDtoV2
        {
            Id = id,
            Name = $"Product {id}",
            Price = id * 10.99m,
            Category = "General",
            Stock = id * 10,
            IsActive = true
        };

        activity?.SetTag("product.found", true);
        activity?.SetTag("product.name", product.Name);
        activity?.SetTag("product.category", product.Category);

        return Ok(product);
    }

    /// <summary>
    /// Cria um novo produto com informações adicionais (v2)
    /// </summary>
    /// <param name="product">Dados do produto</param>
    /// <returns>Produto criado</returns>
    /// <response code="201">Produto criado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cria um novo produto com dados completos",
        Description = "Adiciona um novo produto ao catálogo com categoria, estoque e status (versão 2.0)",
        OperationId = "CreateProduct_V2"
    )]
    [SwaggerResponse(201, "Created", typeof(ProductDtoV2))]
    [SwaggerResponse(400, "Bad Request")]
    public ActionResult<ProductDtoV2> Create([FromBody] CreateProductDtoV2 product)
    {
        _logger.LogInformation("API v2.0 - Criando novo produto: {ProductName}", product.Name);

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return BadRequest(new { message = "Nome do produto é obrigatório", errorCode = "INVALID_NAME" });
        }

        if (string.IsNullOrWhiteSpace(product.Category))
        {
            return BadRequest(new { message = "Categoria do produto é obrigatória", errorCode = "INVALID_CATEGORY" });
        }

        var createdProduct = new ProductDtoV2
        {
            Id = Random.Shared.Next(1, 1000),
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            Stock = product.Stock,
            IsActive = true
        };

        return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
    }

    /// <summary>
    /// Atualiza um produto existente (v2)
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <param name="product">Dados atualizados do produto</param>
    /// <returns>Produto atualizado</returns>
    /// <response code="200">Produto atualizado com sucesso</response>
    /// <response code="404">Produto não encontrado</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPut("{id:int}")]
    [SwaggerOperation(
        Summary = "Atualiza um produto existente",
        Description = "Atualiza todos os dados de um produto existente (versão 2.0)",
        OperationId = "UpdateProduct_V2"
    )]
    [SwaggerResponse(200, "Success", typeof(ProductDtoV2))]
    [SwaggerResponse(404, "Not Found")]
    [SwaggerResponse(400, "Bad Request")]
    public ActionResult<ProductDtoV2> Update(int id, [FromBody] UpdateProductDtoV2 product)
    {
        _logger.LogInformation("API v2.0 - Atualizando produto ID: {ProductId}", id);

        if (id <= 0)
        {
            return NotFound(new { message = "Produto não encontrado", errorCode = "PRODUCT_NOT_FOUND" });
        }

        var updatedProduct = new ProductDtoV2
        {
            Id = id,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            Stock = product.Stock,
            IsActive = product.IsActive
        };

        return Ok(updatedProduct);
    }

    /// <summary>
    /// Remove um produto (v2)
    /// </summary>
    /// <param name="id">ID do produto</param>
    /// <returns>Confirmação de remoção</returns>
    /// <response code="204">Produto removido com sucesso</response>
    /// <response code="404">Produto não encontrado</response>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(
        Summary = "Remove um produto",
        Description = "Remove um produto do catálogo (versão 2.0)",
        OperationId = "DeleteProduct_V2"
    )]
    [SwaggerResponse(204, "No Content")]
    [SwaggerResponse(404, "Not Found")]
    public IActionResult Delete(int id)
    {
        _logger.LogInformation("API v2.0 - Removendo produto ID: {ProductId}", id);

        if (id <= 0)
        {
            return NotFound(new { message = "Produto não encontrado", errorCode = "PRODUCT_NOT_FOUND" });
        }

        return NoContent();
    }
}

/// <summary>
/// DTO de produto versão 2.0 (com informações adicionais)
/// </summary>
public class ProductDtoV2
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

    /// <summary>
    /// Categoria do produto
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade em estoque
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Indica se o produto está ativo
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO para criação de produto versão 2.0
/// </summary>
public class CreateProductDtoV2
{
    /// <summary>
    /// Nome do produto
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Preço do produto
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Categoria do produto
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade em estoque
    /// </summary>
    public int Stock { get; set; }
}

/// <summary>
/// DTO para atualização de produto versão 2.0
/// </summary>
public class UpdateProductDtoV2
{
    /// <summary>
    /// Nome do produto
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Preço do produto
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Categoria do produto
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade em estoque
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Indica se o produto está ativo
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Resposta paginada de produtos
/// </summary>
public class ProductListResponse
{
    /// <summary>
    /// Lista de produtos
    /// </summary>
    public IEnumerable<ProductDtoV2> Items { get; set; } = Array.Empty<ProductDtoV2>();

    /// <summary>
    /// Total de itens
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Página atual
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Tamanho da página
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total de páginas
    /// </summary>
    public int TotalPages { get; set; }
}
