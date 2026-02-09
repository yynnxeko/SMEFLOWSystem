using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.DTOs.Payment
{
    public class SimulatePaymentResponseDto
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty; 
    }
}
