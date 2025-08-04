using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProductSalesApi.Data;
using ProductSalesApi.Dtos;
using ProductSalesApi.Models;
using System.Data;
using System.Text.Json;

namespace ProductSalesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly string _connectionString;

    public ProductsController(AppDbContext context, IWebHostEnvironment env, IConfiguration config)
    {
        _context = context;
        _env = env;
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }


    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        return await _context.Products.ToListAsync();
    }    

    [HttpPost("merge")]
    public async Task<IActionResult> MergeProducts([FromBody] ProductMergeDto dto)
    {
        var productId = await SaveOrUpdateProductAsync(dto);
        await SaveProductImagesAsync(dto, productId);
        return Ok(new { productId });
    }

    private async Task<int> SaveOrUpdateProductAsync(ProductMergeDto dto)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Создаём DataTable на основе ProductTableType
        var productTable = new DataTable();
        productTable.Columns.Add("ProductID", typeof(int));
        productTable.Columns.Add("SKU", typeof(string));
        productTable.Columns.Add("Name", typeof(string));
        productTable.Columns.Add("Category", typeof(string));
        productTable.Columns.Add("Price", typeof(decimal));
        productTable.Columns.Add("QuantityInStock", typeof(int));
        productTable.Columns.Add("Description", typeof(string));
        productTable.Columns.Add("SaleStartDate", typeof(DateTime));
        productTable.Columns.Add("OperationType", typeof(string));

        productTable.Rows.Add(
            dto.ProductID,
            dto.SKU,
            dto.Name,
            dto.Category ?? (object)DBNull.Value,
            dto.Price,
            dto.QuantityInStock,
            dto.Description ?? (object)DBNull.Value,
            dto.SaleStartDate ?? (object)DBNull.Value,
            dto.OperationType
        );

        using var command = new SqlCommand("MergeProducts", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        var param = command.Parameters.AddWithValue("@Products", productTable);
        param.SqlDbType = SqlDbType.Structured;
        param.TypeName = "ProductTableType";

        // Если MergeProducts возвращает ProductID через SELECT SCOPE_IDENTITY()
        var result = await command.ExecuteScalarAsync();
        return dto.ProductID > 0 ? dto.ProductID : Convert.ToInt32(result);
    }


    private async Task SaveProductImagesAsync(ProductMergeDto dto, int productId)
    {
        if (dto.Images == null || dto.Images.Count == 0)
            return;

        var imagePath = Path.Combine(_env.WebRootPath, "images", "products");
        var imageTable = new DataTable();
        imageTable.Columns.Add("SKU", typeof(string));
        imageTable.Columns.Add("ProductID", typeof(int));
        imageTable.Columns.Add("FileName", typeof(string));
        imageTable.Columns.Add("ImagePath", typeof(string));
        imageTable.Columns.Add("UploadedAt", typeof(DateTime));
        imageTable.Columns.Add("ImageOrder", typeof(int));

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var order = 1;

        foreach (var img in dto.Images)
        {
            var original = Path.GetFileName(img.FileName);
            var fileName = $"{dto.SKU}_{timestamp}_{original}";
            var fullPath = Path.Combine(imagePath, fileName);

            var base64 = img.Base64.Contains(",") ? img.Base64.Split(',')[1] : img.Base64;
            var bytes = Convert.FromBase64String(base64);
            await System.IO.File.WriteAllBytesAsync(fullPath, bytes);

            imageTable.Rows.Add(dto.SKU, productId, fileName, fullPath, DateTime.UtcNow, order++);
        }     

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var cmd = new SqlCommand("SaveProductImages", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        var param = cmd.Parameters.AddWithValue("@Images", imageTable);
        param.SqlDbType = SqlDbType.Structured;
        param.TypeName = "ProductImageTableType";

        await cmd.ExecuteNonQueryAsync();
    }


    // GET: api/products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> Get(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();
        return product;
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = product.ProductID }, product);
    }

    // PUT: api/products/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Product updated)
    {
        if (id != updated.ProductID)
            return BadRequest();

        _context.Entry(updated).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("search")]
    public async Task<ActionResult<PagedResult<ProductResultDto>>> Search([FromBody] ProductFilterDto filter)
    {
        var result = new PagedResult<ProductResultDto>();
        var connectionString = _context.Database.GetConnectionString();

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("SearchProducts", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Name", (object?)filter.Name ?? DBNull.Value);
        command.Parameters.AddWithValue("@Category", (object?)filter.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("@PageNumber", filter.PageNumber);
        command.Parameters.AddWithValue("@PageSize", filter.PageSize);
        command.Parameters.AddWithValue("@SortField", (object?)filter.SortField ?? "ProductID");
        command.Parameters.AddWithValue("@SortOrder", filter.SortOrder);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var product = new ProductResultDto
            {
                ProductID = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.IsDBNull(2) ? null : reader.GetString(2),
                Price = reader.GetDecimal(3),
                QuantityInStock = reader.GetInt32(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                SKU = reader.IsDBNull(6) ? null : reader.GetString(6),
                SaleStartDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                Images = new()
            };

            var imagesJson = reader.IsDBNull(8) ? "[]" : reader.GetString(8);

            try
            {
                var images = JsonSerializer.Deserialize<List<ProductImageDto>>(imagesJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                product.Images = images?.Select(i => new ProductImageDto
                {
                    ImageID = i.ImageID,
                    ProductID = product.ProductID,
                    FileName = i.FileName,
                    UploadedAt = i.UploadedAt,
                    ImageOrder = i.ImageOrder,
                    ImageUrl = $"{baseUrl}/images/products/{i.FileName}"
                }).ToList() ?? new();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse image JSON for ProductID {product.ProductID}: {ex.Message}");
            }

            result.Items.Add(product);
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            result.TotalCount = reader.GetInt32(0);
        }

        return Ok(result);
    }

    [HttpGet("check-sku")]
    public async Task<IActionResult> CheckOrReserveSKU([FromQuery] string sku, [FromQuery] bool reserve = false, [FromQuery] string? reservedBy = null)
    {
        var connectionString = _context.Database.GetConnectionString();

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("CheckOrReserveSKU", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@SKU", sku);
        command.Parameters.AddWithValue("@Reserve", reserve ? 1 : 0);
        command.Parameters.AddWithValue("@ReservedBy", (object?)reservedBy ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var response = new
            {
                isTaken = reader.GetInt32(0) == 1,
                productID = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                reserved = reader.GetInt32(2) == 1
            };

            return Ok(response);
        }

        return BadRequest("Unexpected response from CheckOrReserveSKU");
    }

    [HttpGet("reserve-sku")]
    public async Task<IActionResult> ReserveSKU([FromQuery] string? reservedBy = null)
    {
        var connectionString = _context.Database.GetConnectionString();

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("ReserveNextSKU", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@ReservedBy", (object?)reservedBy ?? DBNull.Value);

        var reservedSku = await command.ExecuteScalarAsync();

        return Ok(new { sku = reservedSku?.ToString() });
    }


}
