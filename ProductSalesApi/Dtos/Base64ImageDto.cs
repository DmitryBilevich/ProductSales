public class Base64ImageDto
{
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = "image/jpeg";
    public string Base64 { get; set; } = null!;
}
