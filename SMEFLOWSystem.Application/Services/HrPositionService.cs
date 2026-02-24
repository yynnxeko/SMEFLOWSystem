using AutoMapper;
using SMEFLOWSystem.Application.DTOs.HRDtos;
using SMEFLOWSystem.Application.Interfaces.IRepositories;
using SMEFLOWSystem.Application.Interfaces.IServices;
using SMEFLOWSystem.Core.Entities;
using SMEFLOWSystem.SharedKernel.Interfaces;

namespace SMEFLOWSystem.Application.Services;

public class HrPositionService : IHrPositionService
{
    private const string RoleTenantAdmin = "TenantAdmin";
    private const string RoleManager = "Manager";
    private const string RoleHrManager = "HRManager";

    private readonly IPositionRepository _positionRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public HrPositionService(
        IPositionRepository positionRepo,
        IDepartmentRepository departmentRepo,
        IEmployeeRepository employeeRepo,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _positionRepo = positionRepo;
        _departmentRepo = departmentRepo;
        _employeeRepo = employeeRepo;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<List<PositionDto>> GetByDepartmentAsync(Guid departmentId)
    {
        EnsureHrAccess();
        if (!IsAdmin())
        {
            var myDeptId = await GetManagerDepartmentIdOrThrowAsync();
            if (myDeptId != departmentId) throw new UnauthorizedAccessException("Forbidden");
        }

        var items = await _positionRepo.GetByDepartmentIdAsync(departmentId);
        return _mapper.Map<List<PositionDto>>(items);
    }

    public async Task<PositionDto> CreateAsync(PositionCreateDto request)
    {
        EnsureAdmin();
        var dept = await _departmentRepo.GetByIdAsync(request.DepartmentId);
        if (dept == null) throw new KeyNotFoundException("Department not found");

        var entity = _mapper.Map<Position>(request);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = null;
        entity.IsDeleted = false;
        await _positionRepo.AddAsync(entity);
        return _mapper.Map<PositionDto>(entity);
    }

    public async Task<PositionDto> UpdateAsync(Guid id, PositionUpdateDto request)
    {
        EnsureAdmin();
        var pos = await _positionRepo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Position not found");
        var dept = await _departmentRepo.GetByIdAsync(request.DepartmentId);
        if (dept == null) throw new KeyNotFoundException("Department not found");

        pos.Name = request.Name;
        pos.DepartmentId = request.DepartmentId;
        pos.UpdatedAt = DateTime.UtcNow;
        await _positionRepo.UpdateAsync(pos);
        return _mapper.Map<PositionDto>(pos);
    }

    public async Task DeleteAsync(Guid id)
    {
        EnsureAdmin();
        var pos = await _positionRepo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Position not found");
        var hasEmployees = await _positionRepo.HasEmployeesAsync(id);
        if (hasEmployees)
            throw new ArgumentException("Không thể xóa vì đang có nhân viên sử dụng");
        await _positionRepo.SoftDeleteAsync(pos);
    }

    private void EnsureHrAccess()
    {
        if (_currentUser.UserId == null) throw new UnauthorizedAccessException("Unauthenticated");
        if (!IsAdmin() && !IsManager()) throw new UnauthorizedAccessException("Forbidden");
    }

    private void EnsureAdmin()
    {
        EnsureHrAccess();
        if (!IsAdmin()) throw new UnauthorizedAccessException("Forbidden");
    }

    private bool IsAdmin() => _currentUser.IsInRole(RoleTenantAdmin);
    private bool IsManager() => _currentUser.IsInRole(RoleManager) || _currentUser.IsInRole(RoleHrManager);

    private async Task<Guid> GetManagerDepartmentIdOrThrowAsync()
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Unauthenticated");
        var emp = await _employeeRepo.GetByUserIdAsync(userId);
        if (emp == null || !emp.DepartmentId.HasValue || !emp.PositionId.HasValue)
            throw new UnauthorizedAccessException("Bạn chưa được gán phòng ban/chức vụ");
        return emp.DepartmentId.Value;
    }
}
