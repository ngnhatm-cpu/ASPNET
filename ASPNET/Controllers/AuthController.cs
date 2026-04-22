using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ASPNET.Data;
using ASPNET.Models;
using ASPNET.DTOs;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using ASPNET.Services;

namespace ASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;

    public AuthController(AppDbContext context, IConfiguration config, IEmailService emailService)
    {
        _context = context;
        _config = config;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request)
    {
        if (_context.Users.Any(u => u.Username == request.Username))
            return BadRequest(new { message = "Tên đăng nhập đã tồn tại" });

        if (_context.Users.Any(u => u.Email == request.Email))
            return BadRequest(new { message = "Email đã tồn tại" });

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // Giả sử dùng BCrypt hoặc tương đương
            Role = "Customer"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Balance = user.Balance,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Balance = user.Balance,
                CreatedAt = user.CreatedAt
            }
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
        if (user == null)
            return BadRequest(new { message = "Email không tồn tại trong hệ thống" });

        // Tạo mã OTP 6 số
        var otp = new Random().Next(100000, 999999).ToString();
        user.ResetOtp = otp;
        user.ResetOtpExpiry = DateTime.UtcNow.AddMinutes(10); // Hết hạn sau 10 phút

        await _context.SaveChangesAsync();

        // Gửi mail
        string subject = "Mã xác thực khôi phục mật khẩu - Manga Store";
        string message = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #6200ee; text-align: center;'>Manga Store</h2>
                <p>Chào bạn,</p>
                <p>Chúng tôi nhận được yêu cầu khôi phục mật khẩu của bạn. Vui lòng sử dụng mã OTP dưới đây để tiếp tục:</p>
                <div style='background-color: #f3e5f5; padding: 20px; text-align: center; border-radius: 5px; margin: 20px 0;'>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #6200ee;'>{otp}</span>
                </div>
                <p>Mã này có hiệu lực trong vòng <b>10 phút</b>. Nếu bạn không yêu cầu thay đổi mật khẩu, vui lòng bỏ qua email này.</p>
                <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
                <p style='font-size: 12px; color: #888; text-align: center;'>Đây là email tự động, vui lòng không phản hồi.</p>
            </div>";

        await _emailService.SendEmailAsync(user.Email, subject, message);

        return Ok(new { message = "Mã OTP đã được gửi về Gmail của bạn" });
    }

    [HttpPost("verify-otp")]
    public IActionResult VerifyOtp(VerifyOtpRequest request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email && u.ResetOtp == request.Otp);

        if (user == null || user.ResetOtpExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Mã OTP không chính xác hoặc đã hết hạn" });

        return Ok(new { message = "Mã OTP hợp lệ" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email && u.ResetOtp == request.Otp);

        if (user == null || user.ResetOtpExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Mã OTP không chính xác hoặc đã hết hạn" });

        // Cập nhật mật khẩu mới
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetOtp = null; // Xóa OTP sau khi dùng xong
        user.ResetOtpExpiry = null;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Mật khẩu đã được thay đổi thành công" });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        string keyString = jwtSettings["Key"] ?? "MangaStore_Digital_Super_Secret_Key_2024_Security_Check_Must_Be_Long";
        var key = Encoding.UTF8.GetBytes(keyString);
        
        string durationStr = jwtSettings["DurationInMinutes"] ?? "1440";
        double duration = double.TryParse(durationStr, out var d) ? d : 1440;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(duration),
            Issuer = jwtSettings["Issuer"] ?? "MangaStoreAPI",
            Audience = jwtSettings["Audience"] ?? "MangaStoreUsers",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
