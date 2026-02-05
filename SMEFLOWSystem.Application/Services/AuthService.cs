using Microsoft.Extensions.Configuration;
using ShareKernel.Common.Enum;
using SMEFLOWSystem.Application.DTOs.AuthDtos;
using SMEFLOWSystem.Application.Helpers;
using SMEFLOWSystem.Application.Interfaces.IRepositories;
using SMEFLOWSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Services
{
    public class AuthService
    {
        private readonly ITenantRepository _tenantRepo;
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IUserRoleRepository _userRoleRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly ISubscriptionPlanRepository _planRepo;
        private readonly ITransaction _transaction;
        private readonly IConfiguration _config;

        // Constructor Injection
        public AuthService(
            ITenantRepository tenantRepo,
            IUserRepository userRepo,
            IRoleRepository roleRepo,
            IUserRoleRepository userRoleRepo,
            IOrderRepository orderRepo,
            ICustomerRepository customerRepo,
            ISubscriptionPlanRepository planRepo,
            ITransaction transaction,
            IConfiguration config)
        {
            _tenantRepo = tenantRepo;
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _userRoleRepo = userRoleRepo;
            _orderRepo = orderRepo;
            _customerRepo = customerRepo;
            _planRepo = planRepo;
            _transaction = transaction;
            _config = config;
        }

        public async Task<bool> RegisterTenantAsync(RegisterRequestDto request)
        {            
            var existingUser = await _userRepo.GetUserByEmailAsync(request.AdminEmail);
            if(existingUser != null)
                throw new Exception("Email này đã được sử dụng!");


            // Bao bọc toàn bộ quy trình trong Transaction
            await _transaction.ExecuteAsync(async () =>
            {
                // BƯỚC 1: TẠO TENANT (CÔNG TY)
                // ---------------------------------------------------
                var newTenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = request.CompanyName,
                    Status = StatusEnum.TenantPending, 
                    SubscriptionPlanId = request.SubscriptionPlanId,
                    CreatedAt = DateTime.UtcNow,
                };

                await _tenantRepo.AddAsync(newTenant);


                // BƯỚC 2: TẠO USER ADMIN CHO TENANT ĐÓ
                // ---------------------------------------------------
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = newTenant.Id,
                    FullName = request.AdminFullName,
                    Email = request.AdminEmail,
                    Phone = request.PhoneNumber ?? string.Empty,
                    PasswordHash = AuthHelper.HashPassword(request.Password),
                    IsActive = true,
                    IsVerified = false, // Cần confirm email sau
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepo.AddAsync(adminUser);


                // BƯỚC 3: UPDATE OWNER CHO TENANT
                // ---------------------------------------------------
                // Gán User vừa tạo làm chủ sở hữu (Owner) của Tenant
                newTenant.OwnerUserId = adminUser.Id;
                await _tenantRepo.UpdateAsync(newTenant);


                // BƯỚC 4: GÁN QUYỀN (ROLE) ADMIN CHO USER
                // ---------------------------------------------------
                // Lấy Role "TenantAdmin" từ hệ thống (giả sử tên Role là cố định)
                // Lưu ý: Role này là System Role (TenantId = null)
                var adminRole = await _roleRepo.GetRoleByNameAsync("TenantAdmin");

                if (adminRole == null)
                    throw new Exception("Lỗi hệ thống: Không tìm thấy Role 'TenantAdmin'. Hãy chạy Seed Data trước.");

                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                };

                await _userRoleRepo.AddUserRoleAsync(userRole);


                // BƯỚC 5: TẠO KHÁCH HÀNG ĐẠI DIỆN (INTERNAL CUSTOMER)
                // ---------------------------------------------------
                // Để tạo Order, cần có CustomerId. Ta tạo một Customer nội bộ đại diện cho chính Tenant này.
                var internalCustomer = new Customer
                {
                    Id = Guid.NewGuid(),
                    TenantId = newTenant.Id,
                    Name = request.CompanyName, 
                    Email = request.AdminEmail,
                    Type = "Internal", 
                    CreatedAt = DateTime.UtcNow
                };

                await _customerRepo.AddAsync(internalCustomer);


                // BƯỚC 6: TẠO ĐƠN HÀNG THANH TOÁN (ORDER)
                // ---------------------------------------------------
                // Lấy thông tin gói dịch vụ để tính tiền
                var plan = await _planRepo.GetByIdAsync(request.SubscriptionPlanId);
                if (plan == null) throw new Exception("Gói dịch vụ không tồn tại!");

                var newOrder = new Order
                {
                    Id = Guid.NewGuid(),
                    TenantId = newTenant.Id,
                    CustomerId = internalCustomer.Id,
                    OrderNumber = AuthHelper.GenerateOrderNumber(), 
                    OrderDate = DateTime.UtcNow,
                    Status = StatusEnum.OrderPending,       
                    PaymentStatus = StatusEnum.PaymentPending, 
                    TotalAmount = plan.Price,
                    DiscountAmount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                await _orderRepo.AddAsync(newOrder);

                // KẾT THÚC TRANSACTION: Mọi thứ sẽ được Commit tự động nếu không có Exception
            });

            return true;
        }

        public async Task<string> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepo.GetUserByEmailAsync(request.Email);

            if (user == null)
                throw new Exception("Tài khoản hoặc mật khẩu không chính xác");

            if (!AuthHelper.VerifyPassword(request.Password, user.PasswordHash))
                throw new Exception("Tài khoản hoặc mật khẩu không chính xác");

            if (!user.IsActive)
                throw new Exception("Tài khoản của bạn đã bị khóa.");

            if (user.Tenant.Status == StatusEnum.TenantSuspended)
                throw new Exception("Dịch vụ của công ty đã hết hạn/bị treo.");

            if (user.Tenant.Status == StatusEnum.TenantPending)
                throw new Exception("Công ty chưa kích hoạt dịch vụ (Chưa thanh toán).");

            var token = AuthHelper.GenerateJwtToken(user, _config);
            return token;
        }
    }
}
