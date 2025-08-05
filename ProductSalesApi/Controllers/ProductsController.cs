using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProductSalesApi.Data;
using ProductSalesApi.Dtos;
using ProductSalesApi.Models;
using System.Data;
using System.Text.Json;
using OfficeOpenXml;
using System.Globalization;


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

        // Search parameters
        command.Parameters.AddWithValue("@Name", (object?)filter.Name ?? DBNull.Value);
        command.Parameters.AddWithValue("@Sku", (object?)filter.Sku ?? DBNull.Value);

        // Categories as JSON array
        var categoriesJson = filter.Categories?.Any() == true ?
            JsonSerializer.Serialize(filter.Categories) : null;
        command.Parameters.AddWithValue("@Categories", (object?)categoriesJson ?? DBNull.Value);

        // Price range parameters
        command.Parameters.AddWithValue("@PriceMin", (object?)filter.PriceMin ?? DBNull.Value);
        command.Parameters.AddWithValue("@PriceMax", (object?)filter.PriceMax ?? DBNull.Value);

        // Sale Start Date range parameters
        command.Parameters.AddWithValue("@SaleStartDateMin", (object?)filter.SaleStartDateMin ?? DBNull.Value);
        command.Parameters.AddWithValue("@SaleStartDateMax", (object?)filter.SaleStartDateMax ?? DBNull.Value);


        // Stock ranges as table-valued parameter
        var stockRangesTable = new DataTable();
        stockRangesTable.Columns.Add("MinStock", typeof(int));
        stockRangesTable.Columns.Add("MaxStock", typeof(int));
        stockRangesTable.Columns["MaxStock"].AllowDBNull = true;

        if (filter.StockRanges?.Any() == true)
        {
            foreach (var range in filter.StockRanges)
            {
                stockRangesTable.Rows.Add(range.MinStock, (object?)range.MaxStock ?? DBNull.Value);
            }
        }

        var stockRangesParam = command.Parameters.AddWithValue("@StockRanges", stockRangesTable);
        stockRangesParam.SqlDbType = SqlDbType.Structured;
        stockRangesParam.TypeName = "dbo.StockRangeTableType";

        // Pagination and sorting
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

            // Process the data in SQL
            var result = await ProcessImportData(importSessionId, products);
            result.Success = true;
            result.Message = $"Successfully processed {products.Count} rows from file";
            result.ImportSessionId = importSessionId;

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

    [HttpPost("export")]
    public async Task<IActionResult> ExportProducts([FromBody] ProductExportRequestDto request)
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

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

            List<Product> products;

            if (request.ExportType == "tree")
            {
                // Export tree view products (filtered by category if selected)
                var query = _context.Products.AsQueryable();
                if (!string.IsNullOrEmpty(request.SelectedCategory))
                {
                    query = query.Where(p => p.Category == request.SelectedCategory);
                }
                products = await query.ToListAsync();
            }
            else
            {
                // Export current filtered products
                products = await GetFilteredProducts(request);
            }

            // Add data rows
            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = product.SKU;
                worksheet.Cells[row, 2].Value = product.Name;
                worksheet.Cells[row, 3].Value = product.Category;
                worksheet.Cells[row, 4].Value = product.Price;
                worksheet.Cells[row, 5].Value = product.QuantityInStock;
                worksheet.Cells[row, 6].Value = product.Description;
                worksheet.Cells[row, 7].Value = product.SaleStartDate?.ToString("yyyy-MM-dd");
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


    [HttpGet("import-staging/{importSessionId}")]
    public async Task<ActionResult<ProductImportPagedResultDto>> GetImportStaging(
        Guid importSessionId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortField = "RowNumber",
        [FromQuery] int sortOrder = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("GetProductImportStaging", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@ImportSessionID", importSessionId);
        command.Parameters.AddWithValue("@PageNumber", pageNumber);
        command.Parameters.AddWithValue("@PageSize", pageSize);
        command.Parameters.AddWithValue("@SortField", sortField ?? "RowNumber");
        command.Parameters.AddWithValue("@SortOrder", sortOrder);

        var result = new ProductImportPagedResultDto();

        using var reader = await command.ExecuteReaderAsync();

        // Read staging items
        while (await reader.ReadAsync())
        {
            var item = new ProductImportStagingDto
            {
                StagingID = reader.GetInt32("StagingID"),
                OperationType = reader.GetString("OperationType"),
                SKU = reader.IsDBNull("SKU") ? null : reader.GetString("SKU"),
                Name = reader.GetString("Name"),
                Category = reader.IsDBNull("Category") ? null : reader.GetString("Category"),
                Price = reader.GetDecimal("Price"),
                QuantityInStock = reader.GetInt32("QuantityInStock"),
                Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                SaleStartDate = reader.IsDBNull("SaleStartDate") ? null : reader.GetDateTime("SaleStartDate"),
                ExistingProductID = reader.IsDBNull("ExistingProductID") ? null : reader.GetInt32("ExistingProductID"),
                ValidationErrors = reader.IsDBNull("ValidationErrors") ? null : reader.GetString("ValidationErrors"),
                RowNumber = reader.GetInt32("RowNumber"),
                ModifiedAt = reader.GetDateTime("ModifiedAt"),
                CurrentName = reader.IsDBNull("CurrentName") ? null : reader.GetString("CurrentName"),
                CurrentCategory = reader.IsDBNull("CurrentCategory") ? null : reader.GetString("CurrentCategory"),
                CurrentPrice = reader.IsDBNull("CurrentPrice") ? null : reader.GetDecimal("CurrentPrice"),
                CurrentQuantityInStock = reader.IsDBNull("CurrentQuantityInStock") ? null : reader.GetInt32("CurrentQuantityInStock"),
                CurrentDescription = reader.IsDBNull("CurrentDescription") ? null : reader.GetString("CurrentDescription"),
                CurrentSaleStartDate = reader.IsDBNull("CurrentSaleStartDate") ? null : reader.GetDateTime("CurrentSaleStartDate")
            };

            result.Items.Add(item);
        }

        // Read total count
        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            result.TotalCount = reader.GetInt32("TotalCount");
        }
        else
        {
            result.TotalCount = 0;
        }

        // Read summary
        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            result.Summary = new ProductImportSummaryDto
            {
                TotalRows = reader.GetInt32("TotalRows"),
                NewProducts = reader.GetInt32("NewProducts"),
                UpdatedProducts = reader.GetInt32("UpdatedProducts"),
                ErrorRows = reader.GetInt32("ErrorRows"),
                LastModified = reader.IsDBNull("LastModified") ? null : reader.GetDateTime("LastModified")
            };
        }
        else
        {
            result.Summary = new ProductImportSummaryDto
            {
                TotalRows = 0,
                NewProducts = 0,
                UpdatedProducts = 0,
                ErrorRows = 0,
                LastModified = null
            };
        }

        return Ok(result);
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
            var result = new ProductImportResultDto
            {
                Success = reader.GetInt32("Success") == 1,
                Message = reader.GetString(reader.GetBoolean("Success") ? "Message" : "ErrorMessage"),
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

            var product = new ProductImportRowDto
            {
                RowNumber = rowNumber,
                Sku = GetColumnValue(headers, values, "SKU"),
                Name = GetColumnValue(headers, values, "Name") ?? "",
                Category = GetColumnValue(headers, values, "Category"),
                Price = GetColumnValue(headers, values, "Price") ?? "0",
                QuantityInStock = GetColumnValue(headers, values, "QuantityInStock") ?? "0",
                Description = GetColumnValue(headers, values, "Description"),
                SaleStartDate = GetColumnValue(headers, values, "SaleStartDate")
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

            var product = new ProductImportRowDto
            {
                RowNumber = row,
                Sku = GetExcelColumnValue(worksheet, row, headers, "SKU"),
                Name = name,
                Category = GetExcelColumnValue(worksheet, row, headers, "Category"),
                Price = GetExcelColumnValue(worksheet, row, headers, "Price") ?? "0",
                QuantityInStock = GetExcelColumnValue(worksheet, row, headers, "QuantityInStock") ?? "0",
                Description = GetExcelColumnValue(worksheet, row, headers, "Description"),
                SaleStartDate = GetExcelColumnValue(worksheet, row, headers, "SaleStartDate")
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
        var json = JsonSerializer.Serialize(products, new JsonSerializerOptions
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

    private async Task<List<Product>> GetFilteredProducts(ProductExportRequestDto filter)
    {
        var connectionString = _context.Database.GetConnectionString();

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("SearchProducts", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        // Search parameters
        command.Parameters.AddWithValue("@Name", (object?)filter.Name ?? DBNull.Value);
        command.Parameters.AddWithValue("@Sku", (object?)filter.Sku ?? DBNull.Value);

        // Categories as JSON array
        var categoriesJson = filter.Categories?.Any() == true ?
            JsonSerializer.Serialize(filter.Categories) : null;
        command.Parameters.AddWithValue("@Categories", (object?)categoriesJson ?? DBNull.Value);

        // Price range parameters
        command.Parameters.AddWithValue("@PriceMin", (object?)filter.PriceMin ?? DBNull.Value);
        command.Parameters.AddWithValue("@PriceMax", (object?)filter.PriceMax ?? DBNull.Value);

        // Sale Start Date range parameters
        command.Parameters.AddWithValue("@SaleStartDateMin", (object?)filter.SaleStartDateMin ?? DBNull.Value);
        command.Parameters.AddWithValue("@SaleStartDateMax", (object?)filter.SaleStartDateMax ?? DBNull.Value);

        // Stock ranges as table-valued parameter
        var stockRangesTable = new DataTable();
        stockRangesTable.Columns.Add("MinStock", typeof(int));
        stockRangesTable.Columns.Add("MaxStock", typeof(int));
        stockRangesTable.Columns["MaxStock"].AllowDBNull = true;

        if (filter.StockRanges?.Any() == true)
        {
            foreach (var range in filter.StockRanges)
            {
                stockRangesTable.Rows.Add(range.MinStock, (object?)range.MaxStock ?? DBNull.Value);
            }
        }

        var stockRangesParam = command.Parameters.AddWithValue("@StockRanges", stockRangesTable);
        stockRangesParam.SqlDbType = SqlDbType.Structured;
        stockRangesParam.TypeName = "dbo.StockRangeTableType";

        // Pagination and sorting (set large page size for export)
        command.Parameters.AddWithValue("@PageNumber", 1);
        command.Parameters.AddWithValue("@PageSize", 10000); // Large number for export
        command.Parameters.AddWithValue("@SortField", (object?)filter.SortField ?? "ProductID");
        command.Parameters.AddWithValue("@SortOrder", filter.SortOrder);

        var products = new List<Product>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var product = new Product
            {
                ProductID = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.IsDBNull(2) ? null : reader.GetString(2),
                Price = reader.GetDecimal(3),
                QuantityInStock = reader.GetInt32(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                SKU = reader.IsDBNull(6) ? null : reader.GetString(6),
                SaleStartDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            };

            products.Add(product);
        }

        return products;
    }
}
