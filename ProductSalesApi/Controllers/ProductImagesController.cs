using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProductSalesApi.Dtos;
using System.Data;

namespace ProductSalesApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductImagesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly string _connectionString;

    public ProductImagesController(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    [HttpPost("{productId}/images")]
    public async Task<IActionResult> Upload(int productId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var ext = Path.GetExtension(file.FileName);
        var uniqueName = $"{productId}_{Guid.NewGuid():N}{ext}";

        // Ensure directory exists
        var imagesDir = Path.Combine(_env.WebRootPath, "images", "products");
        Directory.CreateDirectory(imagesDir);

        var path = Path.Combine(imagesDir, uniqueName);

        using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            "INSERT INTO ProductImages (ProductID, FileName) VALUES (@ProductID, @FileName)", conn);

        cmd.Parameters.AddWithValue("@ProductID", productId);
        cmd.Parameters.AddWithValue("@FileName", uniqueName);

        await cmd.ExecuteNonQueryAsync();

        return Ok(new { file = uniqueName });
    }

    [HttpGet("{productId}/images")]
    public async Task<ActionResult<List<ProductImageDto>>> GetImages(int productId)
    {
        var images = new List<ProductImageDto>();

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand("SELECT * FROM ProductImages WHERE ProductID = @ProductID", conn);
        cmd.Parameters.AddWithValue("@ProductID", productId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            images.Add(new ProductImageDto
            {
                ImageID = reader.GetInt32(0),
                ProductID = reader.GetInt32(1),
                FileName = reader.GetString(2),
                UploadedAt = reader.GetDateTime(3)
            });
        }

        return Ok(images);
    }

    [HttpDelete("images/{imageId}")]
    public async Task<IActionResult> Delete(int imageId)
    {
        string? fileName = null;

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using (var cmd = new SqlCommand("SELECT FileName FROM ProductImages WHERE ImageID = @ImageID", conn))
        {
            cmd.Parameters.AddWithValue("@ImageID", imageId);
            var result = await cmd.ExecuteScalarAsync();
            fileName = result as string;
        }

        if (fileName == null)
            return NotFound();

        using (var cmd = new SqlCommand("DELETE FROM ProductImages WHERE ImageID = @ImageID", conn))
        {
            cmd.Parameters.AddWithValue("@ImageID", imageId);
            await cmd.ExecuteNonQueryAsync();
        }

        var path = Path.Combine(_env.WebRootPath, "images", "products", fileName);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);

        return NoContent();
    }
}
