using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMEFLOWSystem.Application.DTOs.Payment
{
    public class PaymentCallbackDto
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; } = "Success";  
        public string? TransactionId { get; set; }  
        public string? Message { get; set; }
    }
}
