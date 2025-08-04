namespace ProductSalesApi.Dtos;

public class ProductImageDto
{
    public int ImageID { get; set; }
    public int ProductID { get; set; }
    public string FileName { get; set; } = null!;
    public DateTime UploadedAt { get; set; }

    public int ImageOrder { get; set; }

    public string ImageUrl { get; set; }
}
