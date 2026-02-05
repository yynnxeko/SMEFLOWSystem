using AutoMapper;
using SMEFLOWSystem.Core.Entities;
using SMEFLOWSystem.Application.DTOs.SubscriptionPlanDtos;

namespace SMEFLOWSystem.Application.Mappings
{
    public class SubscriptionPlanMappingProfile : Profile
    {
        public SubscriptionPlanMappingProfile()
        {
            // Entity -> DTO
            CreateMap<SubscriptionPlan, SubscriptionPlanDto>();

            // DTO -> Entity
            CreateMap<SubscriptionPlanDto, SubscriptionPlan>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())           
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())  
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Tenants, opt => opt.Ignore());    
        }
    }
}