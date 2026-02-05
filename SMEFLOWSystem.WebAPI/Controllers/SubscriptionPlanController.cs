using Microsoft.AspNetCore.Mvc;
using SMEFLOWSystem.Application.DTOs.SubscriptionPlanDtos;
using SMEFLOWSystem.Application.Interfaces.IServices;

namespace SMEFLOWSystem.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionPlanController : Controller
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        public SubscriptionPlanController(ISubscriptionPlanService subscriptionPlanService)
        {
            _subscriptionPlanService = subscriptionPlanService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var subscriptionPlan = await _subscriptionPlanService.GetByIdAsync(id);
            return Ok(subscriptionPlan);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAsync()
        {
            var subscriptionPlans = await _subscriptionPlanService.GetAllAsync();
            return Ok(subscriptionPlans);
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] SubscriptionPlanDto dto)
        {
            var newSubscriptionPlan = await _subscriptionPlanService.AddAsync(dto);
            return Ok(newSubscriptionPlan);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] SubscriptionPlanDto dto)
        {
            var updatedSubscriptionPlan = await _subscriptionPlanService.UpdateAsync(id, dto);
            return Ok(updatedSubscriptionPlan);
        }
    }
}
