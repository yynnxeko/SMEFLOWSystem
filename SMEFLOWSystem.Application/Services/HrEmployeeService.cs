using AutoMapper;
using ShareKernel.Common.Enum;
using SharedKernel.DTOs;
using SMEFLOWSystem.Application.DTOs.HRDtos;
using SMEFLOWSystem.Application.Interfaces.IRepositories;
using SMEFLOWSystem.Application.Interfaces.IServices;
using SMEFLOWSystem.Core.Entities;
using SMEFLOWSystem.SharedKernel.Interfaces;

namespace SMEFLOWSystem.Application.Services;

public class HrEmployeeService : IHrEmployeeService
{
    private const string RoleTenantAdmin = "TenantAdmin";
    private const string RoleManager = "Manager";
    private const string RoleHrManager = "HRManager";

    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IPositionRepository _positionRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public HrEmployeeService(
        IEmployeeRepository employeeRepo,
        IDepartmentRepository departmentRepo,
        IPositionRepository positionRepo,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _employeeRepo = employeeRepo;
        _departmentRepo = departmentRepo;
        _positionRepo = positionRepo;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<EmployeeDto>> GetPagedAsync(EmployeeQueryDto query)
    {
        EnsureHrAccess();

        Guid? departmentId = query.DepartmentId;
        if (IsManagerScoped())
        {
            var myDeptId = await GetManagerDepartmentIdOrThrowAsync();
            if (departmentId.HasValue && departmentId.Value != myDeptId)
                throw new UnauthorizedAccessException("Forbidden");
            departmentId = myDeptId;
        }

        var (items, total) = await _employeeRepo.GetPagedAsync(
            departmentId: departmentId,
            positionId: query.PositionId,
            status: query.Status,
            includeResigned: query.IncludeResigned,
            search: query.Search,
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            sortBy: query.SortBy,
            sortDir: query.SortDir);

        return new PagedResultDto<EmployeeDto>
        {
            Items = _mapper.Map<List<EmployeeDto>>(items),
            TotalCount = total,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<EmployeeDto> GetByIdAsync(Guid id)
    {
        EnsureHrAccess();
        var emp = await _employeeRepo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Employee not found");
        await EnsureEmployeeAccessibleAsync(emp);
        return _mapper.Map<EmployeeDto>(emp);
    }

    public async Task<EmployeeDto> CreateAsync(EmployeeCreateDto request)
    {
        EnsureHrAccess();

        if (IsManagerScoped())
        {
            var myDeptId = await GetManagerDepartmentIdOrThrowAsync();
            if (!request.DepartmentId.HasValue || request.DepartmentId.Value != myDeptId)
                throw new UnauthorizedAccessException("Forbidden");
        }

        await ValidateDepartmentPositionAsync(request.DepartmentId, request.PositionId);

        var entity = _mapper.Map<Employee>(request);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = null;
        entity.IsDeleted = false;
        entity.Phone = request.Phone ?? string.Empty;
        entity.Email = request.Email ?? string.Empty;

        if (string.Equals(entity.Status, StatusEnum.EmployeeResigned, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Không thể tạo nhân viên với trạng thái Resigned");

        await _employeeRepo.AddAsync(entity);
        var reloaded = await _employeeRepo.GetByIdAsync(entity.Id) ?? entity;
        return _mapper.Map<EmployeeDto>(reloaded);
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, EmployeeUpdateDto request)
    {
        EnsureHrAccess();
        var emp = await _employeeRepo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Employee not found");
        await EnsureEmployeeAccessibleAsync(emp);

        if (IsManagerScoped())
        {
            var myDeptId = await GetManagerDepartmentIdOrThrowAsync();
            if (!request.DepartmentId.HasValue || request.DepartmentId.Value != myDeptId)
                throw new UnauthorizedAccessException("Forbidden");
        }

        await ValidateDepartmentPositionAsync(request.DepartmentId, request.PositionId);

        emp.FullName = request.FullName;
        emp.Phone = request.Phone ?? string.Empty;
        emp.Email = request.Email ?? string.Empty;
        emp.DepartmentId = request.DepartmentId;
        emp.PositionId = request.PositionId;
        emp.HireDate = request.HireDate;
        emp.BaseSalary = request.BaseSalary;
        emp.Status = request.Status;
        emp.UserId = request.UserId;

        if (string.Equals(emp.Status, StatusEnum.EmployeeResigned, StringComparison.OrdinalIgnoreCase))
        {
            if (!request.ResignationDate.HasValue)
                throw new ArgumentException("ResignationDate is required when Status=Resigned");
            emp.ResignationDate = request.ResignationDate.Value;
        }
        else
        {
            emp.ResignationDate = null;
        }

        emp.UpdatedAt = DateTime.UtcNow;
        await _employeeRepo.UpdateAsync(emp);

        var reloaded = await _employeeRepo.GetByIdAsync(emp.Id) ?? emp;
        return _mapper.Map<EmployeeDto>(reloaded);
    }

    public async Task DeleteAsync(Guid id)
    {
        EnsureHrAccess();
        var emp = await _employeeRepo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Employee not found");
        await EnsureEmployeeAccessibleAsync(emp);

        emp.Status = StatusEnum.EmployeeResigned;
        emp.ResignationDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        emp.IsDeleted = true;
        emp.UpdatedAt = DateTime.UtcNow;
        await _employeeRepo.UpdateAsync(emp);
    }

    private void EnsureHrAccess()
    {
        if (_currentUser.UserId == null) throw new UnauthorizedAccessException("Unauthenticated");
        if (!IsTenantAdmin() && !IsHrManager() && !IsManager()) throw new UnauthorizedAccessException("Forbidden");
    }

    private bool IsTenantAdmin() => _currentUser.IsInRole(RoleTenantAdmin);
    private bool IsHrManager() => _currentUser.IsInRole(RoleHrManager);
    private bool IsManager() => _currentUser.IsInRole(RoleManager);

    private bool IsManagerScoped() => IsManager() && !IsTenantAdmin() && !IsHrManager();

    private async Task<Guid> GetManagerDepartmentIdOrThrowAsync()
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Unauthenticated");
        var emp = await _employeeRepo.GetByUserIdAsync(userId);
        if (emp == null || !emp.DepartmentId.HasValue || !emp.PositionId.HasValue)
            throw new UnauthorizedAccessException("Bạn chưa được gán phòng ban/chức vụ");
        return emp.DepartmentId.Value;
    }

    private async Task EnsureEmployeeAccessibleAsync(Employee employee)
    {
        if (IsTenantAdmin() || IsHrManager()) return;
        if (IsManagerScoped())
        {
            var myDeptId = await GetManagerDepartmentIdOrThrowAsync();
            if (employee.DepartmentId != myDeptId) throw new UnauthorizedAccessException("Forbidden");
        }
    }

    private async Task ValidateDepartmentPositionAsync(Guid? departmentId, Guid? positionId)
    {
        if (!departmentId.HasValue && !positionId.HasValue) return;
        if (!departmentId.HasValue || !positionId.HasValue)
            throw new ArgumentException("DepartmentId và PositionId phải đi cùng nhau");

        var dept = await _departmentRepo.GetByIdAsync(departmentId.Value);
        if (dept == null) throw new ArgumentException("DepartmentId không tồn tại");

        var pos = await _positionRepo.GetByIdAsync(positionId.Value);
        if (pos == null) throw new ArgumentException("PositionId không tồn tại");
        if (pos.DepartmentId != departmentId.Value) throw new ArgumentException("Position không thuộc Department");
    }
}
