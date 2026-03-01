using SharedKernel.DTOs;
using SMEFLOWSystem.Application.DTOs.SystemDtos;
using SMEFLOWSystem.Application.Interfaces.IRepositories;
using SMEFLOWSystem.Application.Interfaces.IServices.System;

namespace SMEFLOWSystem.Application.Services.System;

public class SystemTenantService : ISystemTenantService
{
    private readonly ITenantRepository _tenantRepository;

    public SystemTenantService(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<PagedResultDto<SystemTenantDto>> GetAllAsync(PagingRequestDto request)
    {
        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;

        var (items, totalCount) = await _tenantRepository.GetPagedIgnoreTenantAsync(pageNumber, pageSize);

        var tenants = items.Select(t => new SystemTenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Status = t.Status,
            SubscriptionEndDate = t.SubscriptionEndDate,
            OwnerUserId = t.OwnerUserId,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();

        return new PagedResultDto<SystemTenantDto>
        {
            Items = tenants,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<SystemTenantDto?> GetByIdAsync(Guid tenantId)
    {
        var tenant = await _tenantRepository.GetByIdIgnoreTenantAsync(tenantId);
        if (tenant == null)
            return null;

        return new SystemTenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Status = tenant.Status,
            SubscriptionEndDate = tenant.SubscriptionEndDate,
            OwnerUserId = tenant.OwnerUserId,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
    }
}
