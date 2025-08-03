using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProductSalesApi.Data;
using ProductSalesApi.Dtos;
using ProductSalesApi.Models;
using System.Data;
using System.Text.Json;

namespace ProductSalesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll()
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ToListAsync();
    }

    // GET: api/orders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> Get(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.OrderID == id);

        if (order == null)
            return NotFound();

        return order;
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<Order>> Create(OrderDto dto)
    {
        var order = new Order
        {
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            CustomerEmail = dto.CustomerEmail,
            OrderDate = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        foreach (var item in dto.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductID);
            if (product == null)
                return BadRequest($"Product ID {item.ProductID} not found");

            order.Items.Add(new OrderItem
            {
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new { orderId = order.OrderID });
    }

    [HttpPost("search")]
    public async Task<ActionResult<List<OrderResultDto>>> Search([FromBody] OrderFilterDto filter)
    {
        var orders = new List<OrderResultDto>();
        var connectionString = _context.Database.GetConnectionString();

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("SearchOrdersWithItems", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@FromDate", (object?)filter.FromDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@ToDate", (object?)filter.ToDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@CustomerName", (object?)filter.CustomerName ?? DBNull.Value);

        var tvp = new DataTable();
        tvp.Columns.Add("ProductID", typeof(int));
        foreach (var id in filter.ProductIDs)
            tvp.Rows.Add(id);

        var tvpParam = command.Parameters.AddWithValue("@ProductIDs", tvp);
        tvpParam.SqlDbType = SqlDbType.Structured;
        tvpParam.TypeName = "ProductIDList";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var order = new OrderResultDto
            {
                OrderID = reader.GetInt32(0),
                CustomerName = reader.GetString(1),
                CustomerPhone = reader.IsDBNull(2) ? null : reader.GetString(2),
                CustomerEmail = reader.IsDBNull(3) ? null : reader.GetString(3),
                OrderDate = reader.GetDateTime(4),
                Items = new()
            };

            var itemsJson = reader.IsDBNull(5) ? "[]" : reader.GetString(5);

            try
            {
                order.Items = JsonSerializer.Deserialize<List<OrderItemResultDto>>(itemsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize items JSON for OrderID {order.OrderID}: {ex.Message}");
            }

            orders.Add(order);
        }

        return Ok(orders);
    }

}
