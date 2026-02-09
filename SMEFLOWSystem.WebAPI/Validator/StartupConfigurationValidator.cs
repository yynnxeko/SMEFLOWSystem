
namespace SMEFLOWSystem.WebAPI.Validator
{
    public static class StartupConfigurationValidator
    {
        public static void Validate(IConfiguration configuration)
        {
            static string GetRequiredConfig(IConfiguration config, string key)
            {
                return config[key] ?? throw new InvalidOperationException($"Missing config: {key}");
            }

            static int GetRequiredIntConfig(IConfiguration config, string key)
            {
                var raw = GetRequiredConfig(config, key);
                if (!int.TryParse(raw, out var value))
                    throw new InvalidOperationException($"Invalid config: {key}");
                return value;
            }

            // JWT
            GetRequiredConfig(configuration, "Jwt:Secret");
            GetRequiredConfig(configuration, "Jwt:Issuer");
            GetRequiredConfig(configuration, "Jwt:Audience");

            // Email
            GetRequiredConfig(configuration, "EmailSettings:SmtpServer");
            GetRequiredIntConfig(configuration, "EmailSettings:SmtpPort");
            GetRequiredConfig(configuration, "EmailSettings:Username");
            GetRequiredConfig(configuration, "EmailSettings:Password");
            GetRequiredConfig(configuration, "EmailSettings:FromName");
            GetRequiredConfig(configuration, "EmailSettings:FromEmail");

            // Payment (nếu dùng VNPay)
            var paymentMode = GetRequiredConfig(configuration, "Payment:Mode");
            var paymentGateway = GetRequiredConfig(configuration, "Payment:Gateway");
            if ((paymentMode == "Sandbox" || paymentMode == "Production") && paymentGateway == "VNPay")
            {
                GetRequiredConfig(configuration, "Payment:VNPay:TmnCode");
                GetRequiredConfig(configuration, "Payment:VNPay:ReturnUrl");
                GetRequiredConfig(configuration, "Payment:VNPay:HashSecret");
                GetRequiredConfig(configuration, "Payment:VNPay:PaymentUrl");
            }
        }
    }
}
