using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.Models;

public class Order
{
    public int Id { get; set; }

    // FK → User
    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Shipped, Delivered, Cancelled

    // Navigation property: 1 Order → nhiều OrderItem
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
