using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProductSalesApi.Data;
using ProductSalesApi.Dtos;
using ProductSalesApi.Models;
using System.Data;

namespace ProductSalesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        return await _context.Products.ToListAsync();
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

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Items.Add(new ProductResultDto
            {
                ProductID = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.IsDBNull(2) ? null : reader.GetString(2),
                Price = reader.GetDecimal(3),
                QuantityInStock = reader.GetInt32(4),
                Description = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            result.TotalCount = reader.GetInt32(0);
        }

        return Ok(result);
    }


}
