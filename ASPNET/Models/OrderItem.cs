using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.Models;

public class OrderItem
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    // FK → Chapter (Mua theo từng chương)
    [Required]
    public int ChapterId { get; set; }
    public Chapter? Chapter { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
}
