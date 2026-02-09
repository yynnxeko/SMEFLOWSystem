using Microsoft.AspNetCore.Mvc;
using SMEFLOWSystem.Application.DTOs.Payment;
using SMEFLOWSystem.Application.Interfaces.IServices;
using System.Runtime.CompilerServices;

namespace SMEFLOWSystem.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : Controller
    {
        private readonly IBillingService _billingService;

        public PaymentController(IBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment(Guid orderId)
        {
            string? clientIp = null;
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
            {
                clientIp = forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim();
            }

            clientIp ??= HttpContext.Connection.RemoteIpAddress?.ToString();

            var url = await _billingService.CreatePaymentUrlAsync(orderId, clientIp);
            return Ok(url);  
        }

        [HttpGet("callback/vnpay")]  
        public async Task<IActionResult> VNPayCallback()
        {
            try
            {
                await _billingService.ProcessVNPayCallbackAsync(Request.Query);

                var isSuccess = Request.Query.TryGetValue("vnp_ResponseCode", out var responseCode)
                    && responseCode.ToString() == "00";

                return Redirect(isSuccess
                    ? "https://your-domain.com/success"
                    : "https://your-domain.com/fail");
            }
            catch
            {
                return Redirect("https://your-domain.com/fail");
            }
        }
    }
}
