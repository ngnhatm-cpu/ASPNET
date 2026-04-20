using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPNET.Data;
using ASPNET.Models;
using ASPNET.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly VNPayService _vnpayService;
    private readonly IConfiguration _configuration;

    public PaymentsController(AppDbContext context, VNPayService vnpayService, IConfiguration configuration)
    {
        _context = context;
        _vnpayService = vnpayService;
        _configuration = configuration;
    }

    [HttpPost("create-topup")]
    [Authorize]
    public async Task<IActionResult> CreateTopup([FromBody] decimal amount)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        if (amount < 5000) 
            return BadRequest(new { message = "Số tiền nạp tối thiểu là 5,000 VNĐ." });

        // Tỷ giá: 10,000 VNĐ = 10 Xu (1,000 VNĐ = 1 Xu)
        decimal xuAmount = amount / 1000;

        var txn = new TopupTransaction
        {
            UserId = userId,
            Amount = amount,
            XuAmount = xuAmount,
            Status = "Pending",
            OrderInfo = $"Nap {xuAmount} Xu vao tai khoan",
            CreatedAt = DateTime.UtcNow
        };

        _context.TopupTransactions.Add(txn);
        await _context.SaveChangesAsync();

        // Tạo URL thanh toán VNPay
        string txnRef = txn.Id.ToString();
        string paymentUrl = _vnpayService.CreatePaymentUrl(HttpContext, amount, txn.OrderInfo, txnRef);

        return Ok(new { paymentUrl });
    }

    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VNPayReturn()
    {
        var vnpayData = Request.Query;
        string hashSecret = _configuration["VNPay:HashSecret"]!;
        
        bool isValidSignature = _vnpayService.ValidateSignature(vnpayData, hashSecret);

        if (isValidSignature)
        {
            string txnRef = vnpayData["vnp_TxnRef"]!;
            string vnp_ResponseCode = vnpayData["vnp_ResponseCode"]!;
            string vnp_TransactionNo = vnpayData["vnp_TransactionNo"]!;

            var txnId = int.Parse(txnRef);
            var txn = await _context.TopupTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == txnId);

            if (txn != null && txn.Status == "Pending")
            {
                txn.VNPayTranId = vnp_TransactionNo;
                txn.ProcessedAt = DateTime.UtcNow;

                if (vnp_ResponseCode == "00")
                {
                    txn.Status = "Success";
                    // Cộng Xu cho người dùng
                    if (txn.User != null)
                    {
                        txn.User.Balance += txn.XuAmount;
                    }
                    await _context.SaveChangesAsync();
                    
                    // Chuyển hướng về trang thành công trên Frontend
                    return Redirect("/payment_result.html?status=success&amount=" + txn.XuAmount);
                }
                else
                {
                    txn.Status = "Failed";
                    await _context.SaveChangesAsync();
                    return Redirect("/payment_result.html?status=fail");
                }
            }
        }

        return Redirect("/payment_result.html?status=error");
    }

    // GET: api/Payments/vnpay-ipn (Server-to-Server)
    [HttpGet("vnpay-ipn")]
    public async Task<IActionResult> VNPayIPN()
    {
        var vnpayData = Request.Query;
        string hashSecret = _configuration["VNPay:HashSecret"]!;
        bool isValidSignature = _vnpayService.ValidateSignature(vnpayData, hashSecret);

        if (!isValidSignature)
            return Ok(new { RspCode = "97", Message = "Invalid Checksum" });

        string txnRef = vnpayData["vnp_TxnRef"]!;
        string vnp_ResponseCode = vnpayData["vnp_ResponseCode"]!;
        string vnp_TransactionNo = vnpayData["vnp_TransactionNo"]!;
        long vnp_Amount = long.Parse(vnpayData["vnp_Amount"]!);

        var txnId = int.Parse(txnRef);
        var txn = await _context.TopupTransactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == txnId);

        if (txn == null)
            return Ok(new { RspCode = "01", Message = "Order info not found" });

        // Kiểm tra số tiền (vnp_Amount trả về đã x100)
        if ((long)(txn.Amount * 100) != vnp_Amount)
            return Ok(new { RspCode = "04", Message = "Invalid amount" });

        if (txn.Status != "Pending")
            return Ok(new { RspCode = "02", Message = "Order already confirmed" });

        // Cập nhật trạng thái
        txn.VNPayTranId = vnp_TransactionNo;
        txn.ProcessedAt = DateTime.UtcNow;

        if (vnp_ResponseCode == "00")
        {
            txn.Status = "Success";
            if (txn.User != null)
            {
                txn.User.Balance += txn.XuAmount;
            }
        }
        else
        {
            txn.Status = "Failed";
        }

        await _context.SaveChangesAsync();
        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    // GET: api/Payments/my-topups
    [HttpGet("my-topups")]
    [Authorize]
    public async Task<IActionResult> GetMyTopups()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var topups = await _context.TopupTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return Ok(topups);
    }

    // GET: api/Payments/admin/all-topups
    [HttpGet("admin/all-topups")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllTopups()
    {
        var topups = await _context.TopupTransactions
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new {
                t.Id,
                username = t.User!.Username,
                t.Amount,
                t.XuAmount,
                t.Status,
                t.VNPayTranId,
                t.CreatedAt,
                t.ProcessedAt
            })
            .ToListAsync();
        return Ok(topups);
    }
}
