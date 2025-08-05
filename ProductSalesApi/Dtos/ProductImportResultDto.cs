namespace ProductSalesApi.Dtos;

public class ProductImportResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int ProcessedCount { get; set; }
    public ProductImportSummaryDto? Summary { get; set; }
    public Guid? ImportSessionId { get; set; }
}