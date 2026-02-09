using Microsoft.AspNetCore.Http;
using SMEFLOWSystem.Application.DTOs.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Interfaces.IServices
{
    public interface IPaymentService
    {
        //Task<bool> ProcessPaymentCallBackAsync(PaymentCallbackDto dto);
        Task<string> CreatePaymentUrlAsync(Guid orderId, string? clientIp = null);  // Trả về URL thanh toán (redirect user đến đó)
        Task<bool> ProcessVNPayCallbackAsync(IQueryCollection query);  // Callback từ VNPay (query params)
        //Task<bool> ProcessMomoCallbackAsync(PaymentCallbackDto dto);
    }
}
