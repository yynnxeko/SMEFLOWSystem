using AutoMapper;
using SharedKernel.DTOs;
using SMEFLOWSystem.Application.DTOs.RoleDtos;
using SMEFLOWSystem.Application.DTOs.UserDtos;
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
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UserDto>> GetAllUserAsync()
        {
            var user = await _userRepository.GetAllUsersAsync();
            return _mapper.Map<IEnumerable<UserDto>>(user);
        }

        public async Task<PagedResultDto<UserDto>> GetAllUsersPagingAsync(PagingRequestDto request)
        {
            var user = await _userRepository.GetAllUserPagingAsync(request);
            
            var userDto =  _mapper.Map<IEnumerable<UserDto>>(user.Items);
            return new PagedResultDto<UserDto>
            {
                Items = userDto,
                TotalCount = user.TotalCount,
                PageNumber = user.PageNumber,
                PageSize = user.PageSize
            };
        }

        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if(user == null)
            {
                throw new ArgumentException($"User with email {email} is not existed");
            }
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                throw new ArgumentException($"User with id {id} is not existed");
            }
            return _mapper.Map<UserDto>(user);
        }

        public async Task<IEnumerable<UserDto>> GetUserByNameAsync(string name)
        { 
            var users = await _userRepository.GetUserByNameAsync(name);
            if(users == null)
            {
                throw new ArgumentException($"User with name {name} is not existed");
            }
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> UpdateAsync(Guid id, UserUpdatedDto user)
        {
            var existingUser = await _userRepository.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                throw new ArgumentException($"User with id {id} is not existed");
            }
            var userEntity = _mapper.Map<User>(user);
            userEntity.Id = existingUser.Id;

            var updatedUser = await _userRepository.UpdateUserAsync(userEntity);
            return _mapper.Map<UserDto>(updatedUser);

        }

        public Task UpdatePasswordAsync(Guid id, string password)
        {
            throw new NotImplementedException();
        }
    }
}
