using SMEFLOWSystem.Core.Entities;

namespace SMEFLOWSystem.Application.Interfaces.IRepositories
{
    public interface IPaymentTransactionRepository
    {
        Task<PaymentTransaction?> GetByGatewayTransactionIdAsync(string gateway, string gatewayTransactionId, bool ignoreTenantFilter = false);
        Task AddAsync(PaymentTransaction transaction);
    }
}
