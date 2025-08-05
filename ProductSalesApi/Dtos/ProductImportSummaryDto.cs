namespace ProductSalesApi.Dtos;

public class ProductImportSummaryDto
{
    public int TotalRows { get; set; }
    public int NewProducts { get; set; }
    public int UpdatedProducts { get; set; }
    public int ErrorRows { get; set; }
    public DateTime? LastModified { get; set; }
}