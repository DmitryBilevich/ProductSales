using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ProductSalesApi.Controllers;

[ApiController]
[Route("api/products/import")]
public class ProductImportController : ControllerBase
{
    private readonly IConfiguration _config;

    public ProductImportController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        var connectionString = _config.GetConnectionString("DefaultConnection");

        // Read CSV
        using var reader = new StreamReader(file.OpenReadStream());
        var header = await reader.ReadLineAsync(); // skip header

        var dataTable = new DataTable();
        dataTable.Columns.AddRange(new[]
        {
            new DataColumn("ProductCode"),
            new DataColumn("Name"),
            new DataColumn("Category"),
            new DataColumn("Price", typeof(decimal)),
            new DataColumn("QuantityInStock", typeof(int)),
            new DataColumn("Description")
        });

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            var values = line?.Split(',');

            if (values is { Length: 6 })
            {
                dataTable.Rows.Add(values[0], values[1], values[2],
                    decimal.Parse(values[3]),
                    int.Parse(values[4]),
                    values[5]);
            }
        }

        // BULK INSERT
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = "ProductImportStaging"
        };

        foreach (DataColumn col in dataTable.Columns)
            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);

        await bulkCopy.WriteToServerAsync(dataTable);

        // MERGE
        using var command = new SqlCommand("EXEC MergeProductsFromStaging", connection);
        await command.ExecuteNonQueryAsync();

        return Ok("Import successful");
    }
}
