using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ASPNET.Services;

public class VNPayService
{
    private readonly IConfiguration _configuration;

    public VNPayService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreatePaymentUrl(HttpContext context, decimal amount, string orderInfo, string txnRef)
    {
        var vnpayConfig = _configuration.GetSection("VNPay");
        string tmnCode = vnpayConfig["TmnCode"] ?? "";
        string hashSecret = vnpayConfig["HashSecret"] ?? "";
        string baseUrl = vnpayConfig["BaseUrl"] ?? "";
        // Tự động xây dựng ReturnUrl dựa trên domain hiện tại của website
        var request = context.Request;
        string returnUrl = $"{request.Scheme}://{request.Host}/api/Payments/vnpay-return";

        // VNPay expects amount in cents (multiply by 100)
        long vnpAmount = (long)(amount * 100);

        var data = new SortedList<string, string>();
        data.Add("vnp_Version", "2.1.0");
        data.Add("vnp_Command", "pay");
        data.Add("vnp_TmnCode", tmnCode);
        data.Add("vnp_Amount", vnpAmount.ToString());
        data.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        data.Add("vnp_CurrCode", "VND");
        data.Add("vnp_IpAddr", "127.0.0.1"); 
        data.Add("vnp_Locale", "vn");
        data.Add("vnp_OrderInfo", orderInfo);
        data.Add("vnp_OrderType", "other");
        data.Add("vnp_ReturnUrl", returnUrl);
        data.Add("vnp_TxnRef", txnRef);

        StringBuilder query = new StringBuilder();
        StringBuilder hashData = new StringBuilder();

        foreach (var kv in data)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                hashData.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }

        string queryString = query.ToString();
        string rawHash = hashData.ToString().TrimEnd('&');
        
        string vnp_SecureHash = HmacSHA512(hashSecret, rawHash).ToUpper();
        queryString += "vnp_SecureHash=" + vnp_SecureHash;

        return baseUrl + "?" + queryString;
    }

    public bool ValidateSignature(IQueryCollection query, string hashSecret)
    {
        string vnp_SecureHash = query["vnp_SecureHash"]!;
        var data = new SortedList<string, string>();

        foreach (var key in query.Keys)
        {
            if (key.StartsWith("vnp_") && key != "vnp_SecureHash" && key != "vnp_SecureHashType")
            {
                data.Add(key, query[key]!);
            }
        }

        StringBuilder hashData = new StringBuilder();
        foreach (var kv in data)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                hashData.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }

        string rawHash = hashData.ToString().TrimEnd('&');
        string checkSum = HmacSHA512(hashSecret, rawHash);

        return checkSum.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
    }

    private string HmacSHA512(string key, string inputData)
    {
        var hash = new StringBuilder();
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString().ToUpper();
    }
}
