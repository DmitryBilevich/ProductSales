namespace ProductSalesApi.Dtos
{
    public class OrderDto
    {
        public string CustomerName { get; set; } = null!;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
    }
}
