namespace ASPNET.DTOs;

public class DashboardStatsDto
{
    public decimal TotalRevenue { get; set; }
    public int NewOrdersCount { get; set; }
    public int TotalUsersCount { get; set; }
    public int TotalChaptersCount { get; set; }
    public List<OrderDto> RecentOrders { get; set; } = new List<OrderDto>();
}
