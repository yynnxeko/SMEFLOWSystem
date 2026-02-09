using Microsoft.Extensions.DependencyInjection;
using SMEFLOWSystem.Application.Interfaces.IServices;
using SMEFLOWSystem.Application.Mappings;
using SMEFLOWSystem.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // AutoMapper profiles
            services.AddAutoMapper(typeof(RoleMappingProfile));
            services.AddAutoMapper(typeof(UserMappingProfile));
            services.AddAutoMapper(typeof(SubscriptionPlanMappingProfile));

            // Services / Use Cases
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IInviteService, InviteService>();
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IBillingService, BillingService>();
            services.AddScoped<IEmailService, EmailService>();
            return services;
        }
    }
}
