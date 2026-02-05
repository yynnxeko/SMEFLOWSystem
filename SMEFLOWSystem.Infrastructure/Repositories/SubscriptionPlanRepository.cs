using Microsoft.EntityFrameworkCore;
using SMEFLOWSystem.Application.Interfaces.IRepositories;
using SMEFLOWSystem.Core.Entities;
using SMEFLOWSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Infrastructure.Repositories
{
    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly SMEFLOWSystemContext _context;

        public SubscriptionPlanRepository(SMEFLOWSystemContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SubscriptionPlan subscriptionPlan)
        {
            await _context.SubscriptionPlans.AddAsync(subscriptionPlan);
            await _context.SaveChangesAsync();
        }

        public async Task<SubscriptionPlan?> AddSubscriptionPlanAsync(SubscriptionPlan subscriptionPlan)
        {
            var result = await _context.SubscriptionPlans.AddAsync(subscriptionPlan);
            await _context.SaveChangesAsync();
            return result.Entity;  
        }

        public async Task<List<SubscriptionPlan>> GetAllAsync()
        {
            var result = await _context.SubscriptionPlans.ToListAsync();
            return result;
        }

        public async Task<SubscriptionPlan?> GetByIdAsync(int id)
        {
            var result = await _context.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id);
            return result;
        }

        public async Task UpdateAsync(SubscriptionPlan subscriptionPlan)
        {
            var existingPlan = await _context.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == subscriptionPlan.Id);
            if (existingPlan != null)
            {
                existingPlan.Name = subscriptionPlan.Name;
                existingPlan.DisplayName = subscriptionPlan.DisplayName;
                existingPlan.Price = subscriptionPlan.Price;
                existingPlan.MaxUsers = subscriptionPlan.MaxUsers;
                existingPlan.Description = subscriptionPlan.Description;
                existingPlan.IsActive = subscriptionPlan.IsActive;
                existingPlan.UpdatedAt = DateTime.UtcNow;
                _context.SubscriptionPlans.Update(existingPlan);
                await _context.SaveChangesAsync();
            }
        }
    }
}
