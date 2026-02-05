using SMEFLOWSystem.Application.DTOs.SubscriptionPlanDtos;
using SMEFLOWSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Interfaces.IServices
{
    public interface ISubscriptionPlanService
    {
        Task<SubscriptionPlan> AddAsync(SubscriptionPlanDto dto);
        Task<SubscriptionPlan> GetByIdAsync(int id);
        Task<IEnumerable<SubscriptionPlan>> GetAllAsync();
        Task<SubscriptionPlan> UpdateAsync(int id, SubscriptionPlanDto dto);
       
    }
}
