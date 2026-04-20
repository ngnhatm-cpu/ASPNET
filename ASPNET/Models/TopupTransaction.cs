using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPNET.Models;

public class TopupTransaction
{
    public int Id { get; set; }

    // FK -> User
    public int UserId { get; set; }
    public User? User { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } // Số tiền VNĐ

    [Column(TypeName = "decimal(18,2)")]
    public decimal XuAmount { get; set; } // Số Xu thực nhận

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Success, Failed

    [MaxLength(100)]
    public string? VNPayTranId { get; set; } // Mã giao dịch từ VNPay

    [MaxLength(255)]
    public string? OrderInfo { get; set; } // Nội dung thanh toán

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
