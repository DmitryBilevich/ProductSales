namespace ProductSalesApi.Dtos;

public class ProductImportPagedResultDto
{
    public List<ProductImportStagingDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public ProductImportSummaryDto? Summary { get; set; }
}