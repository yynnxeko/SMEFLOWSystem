using SharedKernel.DTOs;
using SMEFLOWSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Interfaces.IRepositories
{
    public interface IRoleRepository
    {
        Task<Role?> AddRoleAsync(Role role);
        Task<Role?> UpdateRoleAsync(int id, string name, string description, bool isSystemRole);
        Task<Role?> GetRoleByIdAsync(int id);
        Task<Role?> GetRoleByNameAsync(string name);
        Task<List<Role>> GetAllRolesAsync();
        Task<bool> ExistByNameAsync(string name);
        Task<PagedResultDto<Role>> GetAllRolesPagingAsync(PagingRequestDto request);
        Task<List<User>> GetUsersByRoleIdAsync(int roleId);
        Task<List<Role>> GetByIdsAsync(IEnumerable<int> roleIds);
        Task AddAsync(Role role);
    }
}
