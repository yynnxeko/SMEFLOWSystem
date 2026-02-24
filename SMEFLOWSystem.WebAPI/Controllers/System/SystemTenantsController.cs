using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.DTOs;
using SMEFLOWSystem.Application.DTOs.SystemDtos;
using SMEFLOWSystem.Infrastructure.Data;

namespace SMEFLOWSystem.WebAPI.Controllers.System;

[Route("api/system/tenants")]
[ApiController]
[Authorize(Roles = "SystemAdmin")]
public class SystemTenantsController : ControllerBase
{
    private readonly SMEFLOWSystemContext _db;

    public SystemTenantsController(SMEFLOWSystemContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagingRequestDto request)
    {
        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;
        var skip = (pageNumber - 1) * pageSize;

        var query = _db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();

        var tenants = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(t => new SystemTenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Status = t.Status,
                SubscriptionEndDate = t.SubscriptionEndDate,
                OwnerUserId = t.OwnerUserId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return Ok(new PagedResultDto<SystemTenantDto>
        {
            Items = tenants,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid tenantId)
    {
        var tenant = await _db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.Id == tenantId)
            .Select(t => new SystemTenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Status = t.Status,
                SubscriptionEndDate = t.SubscriptionEndDate,
                OwnerUserId = t.OwnerUserId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (tenant == null)
            return NotFound(new { error = "Không tìm thấy tenant" });

        return Ok(tenant);
    }
}
