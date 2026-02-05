using AutoMapper;
using SMEFLOWSystem.Application.DTOs.SubscriptionPlanDtos;
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
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly IMapper _mapper;
        public SubscriptionPlanService(ISubscriptionPlanRepository subscriptionPlanRepository, IMapper mapper)
        {
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _mapper = mapper;
        }

        public async Task<SubscriptionPlan> AddAsync(SubscriptionPlanDto dto)
        {
            if(dto == null)
            {
                throw new ArgumentNullException(nameof(dto), "SubscriptionPlanDto cannot be null");
            }
            var subscriptionPlan = _mapper.Map<SubscriptionPlan>(dto);
            subscriptionPlan.CreatedAt = DateTime.UtcNow;
            await _subscriptionPlanRepository.AddSubscriptionPlanAsync(subscriptionPlan);
            return subscriptionPlan;
        }

        public async Task<IEnumerable<SubscriptionPlan>> GetAllAsync()
        {
            return await _subscriptionPlanRepository.GetAllAsync();
        }

        public async Task<SubscriptionPlan> GetByIdAsync(int id)
        {
            var subscriptionPlan = await  _subscriptionPlanRepository.GetByIdAsync(id);
            if(subscriptionPlan == null)
            {
                throw new ArgumentException($"SubscriptionPlan with Id {id} not found");
            }
            return subscriptionPlan;
        }

        public async Task<SubscriptionPlan> UpdateAsync(int id, SubscriptionPlanDto dto)
        {
            var existingPlanTask = await _subscriptionPlanRepository.GetByIdAsync(id);
            if (existingPlanTask == null)
            {
                throw new ArgumentException($"SubscriptionPlan with Id {id} not found");
            }
            var updatedPlan = _mapper.Map<SubscriptionPlan>(dto);
            updatedPlan.Id = existingPlanTask.Id;
            await _subscriptionPlanRepository.UpdateAsync(updatedPlan);
            return updatedPlan;
        }
    }
}
