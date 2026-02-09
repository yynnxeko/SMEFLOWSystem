using ShareKernel.Common.Enum;
using SMEFLOWSystem.Application.Helpers;
using SMEFLOWSystem.Application.Interfaces.IRepositories;
using SMEFLOWSystem.Application.Interfaces.IServices;
using SMEFLOWSystem.Core.Entities;
using System;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ISubscriptionPlanRepository _planRepo;

        public OrderService(IOrderRepository orderRepo, ISubscriptionPlanRepository planRepo)
        {
            _orderRepo = orderRepo;
            _planRepo = planRepo;
        }

        public async Task<Order> CreateSubscriptionOrderAsync(Guid tenantId, Guid customerId, int subscriptionPlanId)
        {
            var plan = await _planRepo.GetByIdAsync(subscriptionPlanId);
            if (plan == null) throw new Exception("Gói dịch vụ không tồn tại!");

            var newOrder = new Order
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customerId,
                OrderNumber = AuthHelper.GenerateOrderNumber(),
                OrderDate = DateTime.UtcNow,
                Status = StatusEnum.OrderPending,
                PaymentStatus = StatusEnum.PaymentPending,
                TotalAmount = plan.Price,
                DiscountAmount = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepo.AddAsync(newOrder);
            return newOrder;
        }
    }
}
