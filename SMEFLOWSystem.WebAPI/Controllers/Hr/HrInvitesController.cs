using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMEFLOWSystem.Application.Interfaces.IServices;

namespace SMEFLOWSystem.WebAPI.Controllers.Hr;

public class HrInviteSendRequest
{
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class HrInviteCompleteRequest
{
    public string Token { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

[ApiController]
[Route("api/hr/invites")]
public class HrInvitesController : ControllerBase
{
    private readonly IInviteService _inviteService;
    private readonly SMEFLOWSystem.SharedKernel.Interfaces.ICurrentTenantService _currentTenant;

    public HrInvitesController(IInviteService inviteService, SMEFLOWSystem.SharedKernel.Interfaces.ICurrentTenantService currentTenant)
    {
        _inviteService = inviteService;
        _currentTenant = currentTenant;
    }

    [Authorize]
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] HrInviteSendRequest request)
    {
        var tenantId = _currentTenant.TenantId;
        if (!tenantId.HasValue)
            return StatusCode(403, new { error = "Thiếu TenantId" });

        try
        {
            await _inviteService.SendInviteAsync(tenantId.Value, request.Email, request.RoleId, request.DepartmentId, request.PositionId, request.Message);
            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Unauthenticated onboarding completion (ModuleAccessMiddleware skips unauthenticated requests)
    [AllowAnonymous]
    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] string token)
    {
        try
        {
            var invite = await _inviteService.ValidateInviteTokenAsync(token);
            return Ok(new
            {
                invite.Email,
                invite.TenantId,
                invite.RoleId,
                invite.DepartmentId,
                invite.PositionId,
                invite.ExpiryDate,
                invite.IsUsed
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] HrInviteCompleteRequest request)
    {
        try
        {
            await _inviteService.CompleteOnboardingAsync(request.Token, request.FullName, request.Password, request.Phone);
            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
