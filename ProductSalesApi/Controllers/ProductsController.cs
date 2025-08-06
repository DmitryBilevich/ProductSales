using Microsoft.AspNetCore.Mvc;
using ProductSalesApi.Dtos;
using ProductSalesApi.Services;
using System.Text.Json;

namespace ProductSalesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly JsonStoredProcedureService _jsonService;
    private readonly ProductImportExportService _importExportService;

    public ProductsController(IWebHostEnvironment env, IConfiguration config, JsonStoredProcedureService jsonService, ProductImportExportService importExportService)
    {
        _env = env;
        _jsonService = jsonService;
        _importExportService = importExportService;
    }

    // GET: api/products
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            // Use search endpoint with empty filters to get all products
            var jsonInput = JsonSerializer.Serialize(new { pageSize = 1000 }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Execute JSON stored procedure
            var jsonResult = await _jsonService.ExecuteJsonQueryAsync("GetProductsJson", jsonInput);

            // Return JSON directly to client
            return Content(jsonResult, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
        }
    }

    [HttpPost("merge")]
    public async Task<IActionResult> MergeProducts([FromBody] object productData)
    {
        try
        {
            // Convert input object directly to JSON
            var jsonInput = JsonSerializer.Serialize(productData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Execute JSON stored procedure
            var jsonResult = await _jsonService.ExecuteJsonQueryAsync("MergeProductJson", jsonInput);

            // Return JSON directly to client
            return Content(jsonResult, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
        }
    }

    // GET: api/products/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            // Create JSON input with product ID
            var jsonInput = JsonSerializer.Serialize(new { productID = id }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Execute JSON stored procedure
            var jsonResult = await _jsonService.ExecuteJsonQueryAsync("GetProductByIdJson", jsonInput);

            // Return JSON directly to client
            return Content(jsonResult, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
        }
    }

    // DELETE: api/products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            // Create JSON input with product ID
            var jsonInput = JsonSerializer.Serialize(new { productID = id }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Execute JSON stored procedure
            var jsonResult = await _jsonService.ExecuteJsonQueryAsync("DeleteProductJson", jsonInput);

            // Return JSON directly to client
            return Content(jsonResult, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] object filter)
    {
        try
        {
            // Convert input object directly to JSON
            var jsonInput = JsonSerializer.Serialize(filter, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Execute JSON stored procedure and get JSON result
            var jsonResult = await _jsonService.ExecuteJsonQueryAsync("GetProductsJson", jsonInput);

            // Return JSON directly to client without DTO conversion
            return Content(jsonResult, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    [HttpGet("check-sku")]
    public async Task<IActionResult> CheckOrReserveSKU([FromQuery] string sku, [FromQuery] bool reserve = false, [FromQuery] string? reservedBy = null)
    {
        try
        {
            // Create JSON input
            var jsonInput = JsonSerializer.Serialize(new
            {
                sku = sku,
                reserve = reserve,
                reservedBy = reservedBy
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Execute JSON stored procedure
            var jsonResult = await _jsonService.ExecuteJsonQueryAsync("CheckOrReserveSKUJson", jsonInput);

            // Return JSON directly to client
            return Content(jsonResult, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
        }
    }

    [HttpGet("reserve-sku")]
    public async Task<IActionResult> ReserveSKU([FromQuery] string? reservedBy = null)
    {
        try
        {
            // Create JSON input
            var jsonInput = JsonSerializer.Serialize(new
            {
                reservedBy = reservedBy
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Execute JSON stored procedure
            var jsonResult = await _jsonService.ExecuteJsonQueryAsync("ReserveNextSKUJson", jsonInput);

            // Return JSON directly to client
            return Content(jsonResult, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
        }
    }

    // === BULK IMPORT METHODS ===

    [HttpPost("upload-file")]
    public async Task<ActionResult<ProductImportResultDto>> UploadProductFile(IFormFile file)
    {
        // Use fixed session ID for single user scenario
        var importSessionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var result = await _importExportService.ProcessFileUpload(file, importSessionId);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    [HttpGet("import-staging/{importSessionId}")]
    public async Task<IActionResult> GetImportStaging(
        Guid importSessionId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortField = "RowNumber",
        [FromQuery] int sortOrder = 1)
    {
        try
        {
            // Create JSON input
            var jsonInput = JsonSerializer.Serialize(new
            {
                importSessionId = importSessionId,
                pageNumber = pageNumber,
                pageSize = pageSize,
                sortField = sortField ?? "RowNumber",
                sortOrder = sortOrder
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Execute JSON stored procedure
            var jsonResult = await _jsonService.ExecuteJsonQueryAsync("GetImportStagingDataJson", jsonInput);

            // Return JSON directly to client
            return Content(jsonResult, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
        }
    }

    [HttpPost("process-import/{importSessionId}")]
    public async Task<ActionResult<ProductImportResultDto>> ProcessImport(Guid importSessionId)
    {
        var result = await _importExportService.ProcessFinalImport(importSessionId);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    [HttpDelete("clear-import/{importSessionId}")]
    public async Task<ActionResult<ProductImportResultDto>> ClearImport(Guid importSessionId)
    {
        var result = await _importExportService.ClearImport(importSessionId);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    [HttpPut("update-staging")]
    public async Task<ActionResult<ProductImportResultDto>> UpdateStagingItem([FromBody] ProductImportUpdateRequestDto request)
    {
        var result = await _importExportService.UpdateStagingItem(request);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result);
        }
    }

    [HttpDelete("delete-staging/{stagingId}")]
    public async Task<ActionResult<ProductImportResultDto>> DeleteStagingItem(int stagingId)
    {
        var result = await _importExportService.DeleteStagingItem(stagingId);

        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return result.Message == "Import item not found" ? NotFound(result) : BadRequest(result);
        }
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportProducts([FromBody] object request)
    {
        try
        {
            var excelData = await _importExportService.ExportProducts(request);
            var fileName = $"products_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            return BadRequest($"Export failed: {ex.Message}");
        }
    }

    [HttpPost("export-import")]
    public async Task<IActionResult> ExportImportData([FromBody] ImportExportRequestDto request)
    {
        try
        {
            var excelData = await _importExportService.ExportImportData(request);
            var fileName = $"import_data_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            return BadRequest($"Export failed: {ex.Message}");
        }
    }

    [HttpGet("download-template")]
    public IActionResult DownloadTemplate()
    {
        var filePath = Path.Combine(_env.WebRootPath, "templates", "product-import-template.csv");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("Template file not found");
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "text/csv", "product-import-template.csv");
    }

}
