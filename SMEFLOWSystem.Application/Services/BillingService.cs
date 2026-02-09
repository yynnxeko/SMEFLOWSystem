using Hangfire;
using Microsoft.AspNetCore.Http;
using SMEFLOWSystem.Application.Interfaces.IServices;
using System;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Services
{
    public class BillingService : IBillingService
    {
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public BillingService(
            IPaymentService paymentService,
            IEmailService emailService,
            IBackgroundJobClient backgroundJobClient)
        {
            _paymentService = paymentService;
            _emailService = emailService;
            _backgroundJobClient = backgroundJobClient;
        }

        public Task<string> CreatePaymentUrlAsync(Guid orderId, string? clientIp = null)
            => _paymentService.CreatePaymentUrlAsync(orderId, clientIp);

        public Task<bool> ProcessVNPayCallbackAsync(IQueryCollection query)
            => _paymentService.ProcessVNPayCallbackAsync(query);

        public async Task EnqueuePaymentLinkEmailAsync(Guid orderId, string adminEmail, string companyName, string? clientIp = null)
        {
            var paymentUrl = await _paymentService.CreatePaymentUrlAsync(orderId, clientIp);

            string emailBody = $@"
                    <h3>Chào mừng {companyName} đến với SMEFLOW!</h3>
                    <p>Đơn hàng đăng ký dịch vụ của bạn đã được tạo.</p>
                    <p>Vui lòng click vào link dưới đây để thanh toán và kích hoạt tài khoản:</p>
                    <a href='{paymentUrl}' style='padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none;'>THANH TOÁN NGAY</a>
                    <p>Or copy link: {paymentUrl}</p>";

            _backgroundJobClient.Enqueue(() => _emailService.SendEmailAsync(adminEmail, "Kích hoạt tài khoản Dodo", emailBody));
        }
    }
}
