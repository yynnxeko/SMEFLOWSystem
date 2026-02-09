using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Interfaces.IServices
{
    public interface IBillingService
    {
        Task<string> CreatePaymentUrlAsync(Guid orderId, string? clientIp = null);
        Task<bool> ProcessVNPayCallbackAsync(IQueryCollection query);
        Task EnqueuePaymentLinkEmailAsync(Guid orderId, string adminEmail, string companyName, string? clientIp = null);
    }
}
