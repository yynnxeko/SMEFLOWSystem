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
    public class TenantRepository : ITenantRepository
    {
        private readonly SMEFLOWSystemContext _context;
        public TenantRepository(SMEFLOWSystemContext context)
        {
            _context = context;
        }
        public async Task AddAsync(Core.Entities.Tenant tenant)
        {
            await _context.Tenants.AddAsync(tenant);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tenant tenant)
        {
            var existingTenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == tenant.Id);
            if (existingTenant != null)
            {
                existingTenant.Name = tenant.Name;
                existingTenant.SubscriptionPlanId = tenant.SubscriptionPlanId;
                existingTenant.Status = tenant.Status;
                existingTenant.SubscriptionEndDate = tenant.SubscriptionEndDate;
                existingTenant.OwnerUserId = tenant.OwnerUserId;
                existingTenant.UpdatedAt = DateTime.UtcNow;
                _context.Tenants.Update(existingTenant);
                await _context.SaveChangesAsync();
            }
        }
    }
}
