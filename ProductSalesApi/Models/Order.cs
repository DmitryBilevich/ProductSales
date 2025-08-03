namespace ProductSalesApi.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; } = null!;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
