using SMEFLOWSystem.Application.Interfaces.IRepositories;
using SMEFLOWSystem.Application.Interfaces.IServices;
using SMEFLOWSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Services
{
    public class InviteService : IInviteService
    {
        private readonly IInviteRepository _inviteRepository;
        private readonly IUserRepository _userRepository;

        public InviteService(IInviteRepository inviteRepository, IUserRepository userRepository)
        {
            _inviteRepository = inviteRepository;
            _userRepository = userRepository;
        }

        public Task CompleteOnboardingAsync(string token, string fullName, string password, string? phone)
        {
            throw new NotImplementedException();
        }

        public Task SendInviteAsync(Guid tenantId, string email, int roleId, Guid? departmentId, Guid? positionId, string message)
        {
            throw new NotImplementedException();
        }

        public Task<Invite> ValidateInviteTokenAsync(string token)
        {
            throw new NotImplementedException();
        }
    }
}
