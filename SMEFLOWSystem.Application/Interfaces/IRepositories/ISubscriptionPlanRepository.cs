using SMEFLOWSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Interfaces.IRepositories
{
    public interface ISubscriptionPlanRepository
    {
        Task<SubscriptionPlan?> AddSubscriptionPlanAsync(SubscriptionPlan subscriptionPlan);
        Task<SubscriptionPlan?> GetByIdAsync(int id);
        Task<List<SubscriptionPlan>> GetAllAsync();
        Task UpdateAsync(SubscriptionPlan subscriptionPlan);
        Task AddAsync(SubscriptionPlan subscriptionPlan);
    }
}
