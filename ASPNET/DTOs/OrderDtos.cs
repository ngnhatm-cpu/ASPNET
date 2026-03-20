namespace ASPNET.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int ChapterId { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public string MangaTitle { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

public class CreateOrderRequest
{
    public List<int> ChapterIds { get; set; } = new();
}
