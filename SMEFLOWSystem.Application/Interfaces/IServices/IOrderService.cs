using SMEFLOWSystem.Core.Entities;
using System;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Interfaces.IServices
{
    public interface IOrderService
    {
        Task<Order> CreateSubscriptionOrderAsync(Guid tenantId, Guid customerId, int subscriptionPlanId);
    }
}
