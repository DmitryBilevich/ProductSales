using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ProductSalesApi.Dtos;
using ProductSalesApi.Services;
using System.Data;
using System.Text.Json;
using OfficeOpenXml;
using System.Globalization;

namespace ProductSalesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly string _connectionString;
    private readonly JsonStoredProcedureService _jsonService;

    public ProductsController(IWebHostEnvironment env, IConfiguration config, JsonStoredProcedureService jsonService)
    {
        _env = env;
        _connectionString = config.GetConnectionString("DefaultConnection")!;
        _jsonService = jsonService;
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

    // POST: api/products - Use merge endpoint instead
    // [HttpPost] - Removed in favor of /merge endpoint with JSON approach

    // PUT: api/products/5 - Use merge endpoint instead
    // [HttpPut] - Removed in favor of /merge endpoint with JSON approach

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
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProductImportResultDto
            {
                Success = false,
                Message = "No file uploaded"
            });
        }

        // 5MB limit
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new ProductImportResultDto
            {
                Success = false,
                Message = "File size exceeds 5MB limit"
            });
        }

        var allowedExtensions = new[] { ".xlsx", ".xls", ".csv" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(new ProductImportResultDto
            {
                Success = false,
                Message = "Only Excel (.xlsx, .xls) and CSV files are supported"
            });
        }

        try
        {
            // Use fixed session ID for single user scenario
            var importSessionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var products = new List<ProductImportRowDto>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            if (fileExtension == ".csv")
            {
                products = await ParseCsvFile(stream);
            }
            else
            {
                products = await ParseExcelFile(stream);
            }

            if (products.Count == 0)
            {
                return BadRequest(new ProductImportResultDto
                {
                    Success = false,
                    Message = "No valid data found in file"
                });
            }

            // Process the data using JSON stored procedure
            var importRequest = new
            {
                importSessionId = importSessionId,
                products = products.Select(p => new
                {
                    sku = p.Sku,
                    name = p.Name,
                    category = p.Category,
                    price = decimal.TryParse(p.Price, out var priceVal) ? priceVal : 0m,
                    quantityInStock = int.TryParse(p.QuantityInStock, out var qtyVal) ? qtyVal : 0,
                    description = p.Description,
                    saleStartDate = ParseSaleStartDate(p.SaleStartDate),
                    operationType = "Insert" // Default operation type for uploads
                }).ToList()
            };

            var jsonInput = JsonSerializer.Serialize(importRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Use the staging-based stored procedure instead of direct processing
            var result = await ProcessImportData(importSessionId, products);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ProductImportResultDto
            {
                Success = false,
                Message = $"Error processing file: {ex.Message}"
            });
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
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("ProcessFinalImport", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@ImportSessionID", importSessionId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var success = reader.GetInt32("Success") == 1;
            var result = new ProductImportResultDto
            {
                Success = success,
                Message = reader.GetString(success ? "Message" : "ErrorMessage"),
                ProcessedCount = reader.GetInt32("ProcessedCount")
            };

            return Ok(result);
        }

        return BadRequest(new ProductImportResultDto
        {
            Success = false,
            Message = "Unexpected error processing import"
        });
    }

    [HttpDelete("clear-import/{importSessionId}")]
    public async Task<ActionResult<ProductImportResultDto>> ClearImport(Guid importSessionId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("ClearProductImportStaging", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@ImportSessionID", importSessionId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var clearedCount = reader.GetInt32("ClearedCount");
            var message = reader.GetString("Message");

            return Ok(new ProductImportResultDto
            {
                Success = true,
                Message = message,
                ProcessedCount = clearedCount
            });
        }

        return BadRequest(new ProductImportResultDto
        {
            Success = false,
            Message = "Error clearing import data"
        });
    }

    [HttpPut("update-staging")]
    public async Task<ActionResult<ProductImportResultDto>> UpdateStagingItem([FromBody] ProductImportUpdateRequestDto request)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("UpdateProductImportStaging", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@StagingID", request.StagingID);
        command.Parameters.AddWithValue("@SKU", (object?)request.SKU ?? DBNull.Value);
        command.Parameters.AddWithValue("@Name", request.Name);
        command.Parameters.AddWithValue("@Category", (object?)request.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("@Price", request.Price);
        command.Parameters.AddWithValue("@QuantityInStock", request.QuantityInStock);
        command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@SaleStartDate", (object?)request.SaleStartDate ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var success = reader.GetInt32("Success") == 1;
            var message = reader.GetString("Message");

            return Ok(new ProductImportResultDto
            {
                Success = success,
                Message = message,
                ProcessedCount = success ? 1 : 0
            });
        }

        return BadRequest(new ProductImportResultDto
        {
            Success = false,
            Message = "Error updating staging item"
        });
    }

    [HttpDelete("delete-staging/{stagingId}")]
    public async Task<ActionResult<ProductImportResultDto>> DeleteStagingItem(int stagingId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("DELETE FROM ProductImportStaging WHERE StagingID = @StagingID", connection);
        command.Parameters.AddWithValue("@StagingID", stagingId);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Ok(new ProductImportResultDto
            {
                Success = true,
                Message = "Import item deleted successfully",
                ProcessedCount = 1
            });
        }
        else
        {
            return NotFound(new ProductImportResultDto
            {
                Success = false,
                Message = "Import item not found"
            });
        }
    }

    [HttpPost("export")]
    public async Task<IActionResult> ExportProducts([FromBody] object request)
    {
        try
        {
            // Convert request to JSON and get product data from stored procedure
            var jsonInput = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Get products data as JSON from stored procedure
            var jsonResult = await _jsonService.ExecuteJsonQueryAsync("GetProductsForExportJson", jsonInput);

            // Deserialize the products for Excel generation
            var products = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonResult, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (products == null || products.Count == 0)
            {
                return BadRequest("No products found for export");
            }

            // Create Excel file
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Products");

            // Add headers
            worksheet.Cells[1, 1].Value = "SKU";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Category";
            worksheet.Cells[1, 4].Value = "Price";
            worksheet.Cells[1, 5].Value = "QuantityInStock";
            worksheet.Cells[1, 6].Value = "Description";
            worksheet.Cells[1, 7].Value = "SaleStartDate";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Add data rows
            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = GetJsonValue(product, "sku");
                worksheet.Cells[row, 2].Value = GetJsonValue(product, "name");
                worksheet.Cells[row, 3].Value = GetJsonValue(product, "category");
                worksheet.Cells[row, 4].Value = GetJsonValue(product, "price");
                worksheet.Cells[row, 5].Value = GetJsonValue(product, "quantityInStock");
                worksheet.Cells[row, 6].Value = GetJsonValue(product, "description");

                // Handle date formatting
                var dateValue = GetJsonValue(product, "saleStartDate");
                if (dateValue != null && DateTime.TryParse(dateValue.ToString(), out var saleDate))
                {
                    worksheet.Cells[row, 7].Value = saleDate.ToString("yyyy-MM-dd");
                }
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            var fileName = $"products_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            return BadRequest($"Export failed: {ex.Message}");
        }
    }

    private static object? GetJsonValue(Dictionary<string, object> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value : null;
    }

    [HttpPost("export-import")]
    public async Task<IActionResult> ExportImportData([FromBody] ImportExportRequestDto request)
    {
        try
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Import Data");

            // Add headers
            worksheet.Cells[1, 1].Value = "SKU";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Category";
            worksheet.Cells[1, 4].Value = "Price";
            worksheet.Cells[1, 5].Value = "QuantityInStock";
            worksheet.Cells[1, 6].Value = "Description";
            worksheet.Cells[1, 7].Value = "SaleStartDate";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Get import data from staging table
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                SELECT SKU, Name, Category, Price, QuantityInStock, Description, SaleStartDate
                FROM ProductImportStaging 
                WHERE ImportSessionID = @ImportSessionID
                ORDER BY RowNumber", connection);

            command.Parameters.AddWithValue("@ImportSessionID", request.ImportSessionId);

            using var reader = await command.ExecuteReaderAsync();
            int row = 2;

            while (await reader.ReadAsync())
            {
                worksheet.Cells[row, 1].Value = reader.IsDBNull("SKU") ? null : reader.GetString("SKU");
                worksheet.Cells[row, 2].Value = reader.GetString("Name");
                worksheet.Cells[row, 3].Value = reader.IsDBNull("Category") ? null : reader.GetString("Category");
                worksheet.Cells[row, 4].Value = reader.GetDecimal("Price");
                worksheet.Cells[row, 5].Value = reader.GetInt32("QuantityInStock");
                worksheet.Cells[row, 6].Value = reader.IsDBNull("Description") ? null : reader.GetString("Description");
                worksheet.Cells[row, 7].Value = reader.IsDBNull("SaleStartDate") ? null : reader.GetDateTime("SaleStartDate").ToString("yyyy-MM-dd");
                row++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            stream.Position = 0;

            var fileName = $"import_data_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(stream.ToArray(),
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

    // === HELPER METHODS ===

    private async Task<List<ProductImportRowDto>> ParseCsvFile(MemoryStream stream)
    {
        var products = new List<ProductImportRowDto>();
        stream.Position = 0;

        using var reader = new StreamReader(stream);
        var headerLine = await reader.ReadLineAsync();

        if (string.IsNullOrEmpty(headerLine))
            return products;

        var headers = headerLine.Split(',').Select(h => h.Trim().Trim('"')).ToArray();
        var rowNumber = 1; // Start from 1 (header is row 0)

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split(',').Select(v => v.Trim().Trim('"')).ToArray();

            var saleStartDateValue = GetColumnValue(headers, values, "SaleStartDate");            

            var product = new ProductImportRowDto
            {
                RowNumber = rowNumber,
                Sku = GetColumnValue(headers, values, "SKU"),
                Name = GetColumnValue(headers, values, "Name") ?? "",
                Category = GetColumnValue(headers, values, "Category"),
                Price = GetColumnValue(headers, values, "Price") ?? "0",
                QuantityInStock = GetColumnValue(headers, values, "QuantityInStock") ?? "0",
                Description = GetColumnValue(headers, values, "Description"),
                SaleStartDate = saleStartDateValue
            };

            if (!string.IsNullOrEmpty(product.Name))
            {
                products.Add(product);
            }
        }

        return products;
    }

    private async Task<List<ProductImportRowDto>> ParseExcelFile(MemoryStream stream)
    {
        var products = new List<ProductImportRowDto>();

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet == null || worksheet.Dimension == null)
            return products;

        var rowCount = worksheet.Dimension.Rows;
        var colCount = worksheet.Dimension.Columns;

        // Read headers from first row
        var headers = new Dictionary<string, int>();
        for (int col = 1; col <= colCount; col++)
        {
            var header = worksheet.Cells[1, col].Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(header))
            {
                headers[header] = col;
            }
        }

        // Read data rows
        for (int row = 2; row <= rowCount; row++)
        {
            var name = GetExcelColumnValue(worksheet, row, headers, "Name");

            if (string.IsNullOrEmpty(name)) continue;

            var saleStartDateValue = GetExcelColumnValue(worksheet, row, headers, "SaleStartDate");            

            var product = new ProductImportRowDto
            {
                RowNumber = row,
                Sku = GetExcelColumnValue(worksheet, row, headers, "SKU"),
                Name = name,
                Category = GetExcelColumnValue(worksheet, row, headers, "Category"),
                Price = GetExcelColumnValue(worksheet, row, headers, "Price") ?? "0",
                QuantityInStock = GetExcelColumnValue(worksheet, row, headers, "QuantityInStock") ?? "0",
                Description = GetExcelColumnValue(worksheet, row, headers, "Description"),
                SaleStartDate = saleStartDateValue
            };

            products.Add(product);
        }

        return products;
    }

    private string? GetColumnValue(string[] headers, string[] values, string columnName)
    {
        var index = Array.FindIndex(headers, h => string.Equals(h, columnName, StringComparison.OrdinalIgnoreCase));

        if (index >= 0 && index < values.Length)
        {
            var value = values[index];
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private string? GetExcelColumnValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string columnName)
    {
        if (headers.TryGetValue(columnName, out int col))
        {
            var cellValue = worksheet.Cells[row, col].Value;

            if (cellValue is DateTime dateValue && columnName == "SaleStartDate")
            {
                return dateValue.ToString("yyyy-MM-dd");
            }

            return cellValue?.ToString()?.Trim();
        }

        return null;
    }

    private async Task<ProductImportResultDto> ProcessImportData(Guid importSessionId, List<ProductImportRowDto> products)
    {
        // Convert the raw import data to proper format with parsed dates
        var processedProducts = products.Select(p => new
        {
            rowNumber = p.RowNumber,
            sku = p.Sku,
            name = p.Name,
            category = p.Category,
            price = p.Price,
            quantityInStock = p.QuantityInStock,
            description = p.Description,
            saleStartDate = ParseSaleStartDate(p.SaleStartDate)?.ToString("yyyy-MM-dd HH:mm:ss") // Convert to SQL-friendly format
        }).ToList();

        var json = JsonSerializer.Serialize(processedProducts, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("ProcessProductImportData", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@ImportSessionID", importSessionId);
        command.Parameters.AddWithValue("@ImportData", json);

        using var reader = await command.ExecuteReaderAsync();

        var result = new ProductImportResultDto { Success = true };

        if (await reader.ReadAsync())
        {
            result.Summary = new ProductImportSummaryDto
            {
                TotalRows = reader.GetInt32("TotalRows"),
                NewProducts = reader.GetInt32("NewProducts"),
                UpdatedProducts = reader.GetInt32("UpdatedProducts"),
                ErrorRows = reader.GetInt32("ErrorRows")
            };
        }

        return result;
    }

    private static DateTime? ParseSaleStartDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        // First, try to parse as Excel serial date number
        if (double.TryParse(dateString, out var serialNumber))
        {
            try
            {
                // Excel base date is January 1, 1900, but Excel incorrectly treats 1900 as a leap year
                // So we need to account for this bug in Excel's date system
                var excelBaseDate = new DateTime(1900, 1, 1);

                // Excel serial number 1 = January 1, 1900
                // But we need to subtract 2 because:
                // 1. Excel counts January 1, 1900 as day 1 (not day 0)
                // 2. Excel incorrectly includes February 29, 1900 as a valid date
                var actualDate = excelBaseDate.AddDays(serialNumber - 2);

                // Sanity check: make sure the date is reasonable (between 1900 and 2100)
                if (actualDate.Year >= 1900 && actualDate.Year <= 2100)
                {
                    return actualDate;
                }
            }
            catch
            {
                // Fall through to string parsing if serial number conversion fails
            }
        }

        // Try different date formats commonly found in CSV/Excel files
        var formats = new[]
        {
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "M/d/yyyy",
            "d/M/yyyy",
            "yyyy/MM/dd",
            "dd-MM-yyyy",
            "MM-dd-yyyy",
            "yyyy.MM.dd",
            "dd.MM.yyyy"
        };

        // Try parsing with invariant culture and common formats
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }
        }

        // If exact format parsing fails, try general parsing with invariant culture
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var generalResult))
        {
            return generalResult;
        }

        // Last resort: try parsing with current culture
        if (DateTime.TryParse(dateString, out var cultureResult))
        {
            return cultureResult;
        }

        return null;
    }

}
