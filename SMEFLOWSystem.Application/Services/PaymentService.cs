using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ShareKernel.Common.Enum;
using SMEFLOWSystem.Application.Helpers;
using SMEFLOWSystem.Application.Interfaces.IRepositories;
using SMEFLOWSystem.Application.Interfaces.IServices;
using SMEFLOWSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VNPAY.NET;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET.Utilities;

namespace SMEFLOWSystem.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private const string GatewayVNPay = "VNPay";

        private readonly IBillingOrderRepository _billingOrderRepo;
        private readonly ITenantRepository _tenantRepo;
        private readonly IPaymentTransactionRepository _paymentTransactionRepo;
        private readonly IBillingOrderModuleRepository _billingOrderModuleRepo;
        private readonly IModuleSubscriptionRepository _moduleSubscriptionRepo;
        private readonly IEmailService _emailService;
        private readonly ITransaction _transaction;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IVnpay _vnpay;

        public PaymentService(
            IBillingOrderRepository billingOrderRepo,
            ITenantRepository tenantRepo,
            IPaymentTransactionRepository paymentTransactionRepo,
            IBillingOrderModuleRepository billingOrderModuleRepo,
            IModuleSubscriptionRepository moduleSubscriptionRepo,
            IEmailService emailService,
            ITransaction transaction,
            IBackgroundJobClient backgroundJobClient,
            IConfiguration configuration,
            IUserRepository userRepo,
            IHttpContextAccessor httpContextAccessor,
            IVnpay vnpay)
        {
            _billingOrderRepo = billingOrderRepo;
            _tenantRepo = tenantRepo;
            _paymentTransactionRepo = paymentTransactionRepo;
            _billingOrderModuleRepo = billingOrderModuleRepo;
            _moduleSubscriptionRepo = moduleSubscriptionRepo;
            _emailService = emailService;
            _transaction = transaction;
            _config = configuration;
            _backgroundJobClient = backgroundJobClient;
            _userRepo = userRepo;
            _httpContextAccessor = httpContextAccessor;
            _vnpay = vnpay;
        }

        public async Task<string> CreatePaymentUrlAsync(Guid orderId, string? clientIp = null)
        {
            var billingOrder = await _billingOrderRepo.GetByIdIgnoreTenantAsync(orderId);
            if (billingOrder == null) throw new Exception("Không tìm thấy đơn thanh toán");

            if (!string.Equals(billingOrder.PaymentStatus, StatusEnum.PaymentPending, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(billingOrder.Status, StatusEnum.OrderPending, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Đơn thanh toán không ở trạng thái chờ thanh toán");
            }

            // Lấy IP thực từ HttpContext nếu chưa có (giống reference dùng NetworkHelper)
            if (string.IsNullOrWhiteSpace(clientIp))
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    try { clientIp = NetworkHelper.GetIpAddress(httpContext); }
                    catch { clientIp = "127.0.0.1"; }
                }
                else
                {
                    clientIp = "127.0.0.1";
                }
            }

            var gateway = _config["Payment:Gateway"] ?? throw new Exception("Missing config: Payment:Gateway");
            if (gateway == "VNPay")
            {
                return CreateVNPayUrl(billingOrder, clientIp);
            }
            throw new Exception($"Unsupported payment gateway: {gateway}");
        }

        public async Task<string?> ProcessVNPayCallbackAsync(IQueryCollection query)
        {
            // Initialize VNPAY.NET library for signature verification
            var tmnCode = _config["Payment:VNPay:TmnCode"] ?? throw new Exception("Missing config: Payment:VNPay:TmnCode");
            var hashSecret = _config["Payment:VNPay:HashSecret"] ?? throw new Exception("Missing config: Payment:VNPay:HashSecret");
            var vnpBaseUrl = _config["Payment:VNPay:BaseUrl"] ?? throw new Exception("Missing config: Payment:VNPay:BaseUrl");
            var vnpCallbackUrl = _config["Payment:VNPay:CallbackUrl"] ?? string.Empty;

            _vnpay.Initialize(tmnCode, hashSecret, vnpBaseUrl, vnpCallbackUrl);

            // Use VNPAY.NET library to verify signature and parse result
            PaymentResult result;
            try
            {
                result = _vnpay.GetPaymentResult(query);
            }
            catch (ArgumentException)
            {
                return null;
            }

            if (!Guid.TryParse(result.PaymentId, out var orderId))
                return null;

            var gatewayTransactionId = result.VnpayTransactionId.ToString();
            var isSuccess = result.IsSuccess;
            var status = isSuccess ? "Success" : "Failed";

            // Collect raw data for audit
            var rawData = query
                .Where(q => !string.IsNullOrWhiteSpace(q.Key) && q.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(q => q.Key, q => q.Value.ToString());

            // Callback has no tenant context -> bypass tenant filters when loading order.
            var orderForTenant = await _billingOrderRepo.GetByIdIgnoreTenantAsync(orderId);
            if (orderForTenant == null) throw new Exception("Không tìm thấy đơn thanh toán");

            // Validate merchant code if present
            if (query.ContainsKey("vnp_TmnCode"))
            {
                var tmn = query["vnp_TmnCode"].ToString();
                if (!string.IsNullOrWhiteSpace(tmn) && !string.Equals(tmn, tmnCode, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            // Validate amount
            var amountRaw = query.ContainsKey("vnp_Amount") ? query["vnp_Amount"].ToString() : null;
            if (string.IsNullOrWhiteSpace(amountRaw))
                throw new Exception("Missing vnp_Amount");

            if (!long.TryParse(amountRaw, out var amountMinor) || amountMinor <= 0)
                throw new Exception("Invalid vnp_Amount");

            var amount = amountMinor / 100m;

            // Validate amount matches order payable amount
            var discount = orderForTenant.DiscountAmount ?? 0m;
            var expectedPayable = orderForTenant.TotalAmount - discount;
            if (expectedPayable <= 0m)
                throw new Exception("Đơn thanh toán không hợp lệ (số tiền phải > 0)");
            var expectedMinor = checked((long)decimal.Round(expectedPayable * 100m, 0, MidpointRounding.AwayFromZero));
            if (amountMinor != expectedMinor)
                throw new Exception("Số tiền thanh toán không khớp đơn hàng");

            // Process transactionally
            var shouldEnqueueActivation = false;
            await _transaction.ExecuteAsync(async () =>
            {
                var existingInside = await _paymentTransactionRepo.GetByGatewayTransactionIdAsync(
                    gateway: GatewayVNPay,
                    gatewayTransactionId: gatewayTransactionId,
                    ignoreTenantFilter: true);
                if (existingInside != null) return;

                var order = await _billingOrderRepo.GetByIdIgnoreTenantAsync(orderId);
                if (order == null) throw new Exception("Không tìm thấy đơn thanh toán");

                var paymentTransaction = new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = order.TenantId,
                    BillingOrderId = order.Id,
                    Gateway = GatewayVNPay,
                    GatewayTransactionId = gatewayTransactionId,
                    GatewayResponseCode = query.ContainsKey("vnp_ResponseCode") ? query["vnp_ResponseCode"].ToString() : "unknown",
                    Amount = amount,
                    Status = status,
                    RawData = JsonConvert.SerializeObject(rawData),
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow
                };

                await _paymentTransactionRepo.AddAsync(paymentTransaction);

                if (isSuccess)
                {
                    if (BillingStateMachine.CanSetPaymentToPaid(order.PaymentStatus)
                        && BillingStateMachine.CanSetOrderToCompleted(order.Status))
                    {
                        order.PaymentStatus = StatusEnum.PaymentPaid;
                        order.Status = StatusEnum.OrderCompleted;
                        shouldEnqueueActivation = true;
                    }
                }
                else
                {
                    if (BillingStateMachine.CanSetPaymentToFailed(order.PaymentStatus)
                        && BillingStateMachine.CanSetOrderToCancelled(order.Status))
                    {
                        order.PaymentStatus = StatusEnum.PaymentFailed;
                        order.Status = StatusEnum.OrderCancelled;
                    }
                }
                await _billingOrderRepo.UpdateIgnoreTenantAsync(order);
            });

            // Nếu thanh toán thành công, dùng Hangfire để active Tenant và Owner (background job để không block callback)
            if (shouldEnqueueActivation)
            {
                _backgroundJobClient.Enqueue(() => ActivateTenantAfterPaymentAsync(orderId, gatewayTransactionId));
            }

            return status;
        }

        public async Task<string> BuildSimulatedVNPaySuccessQueryStringAsync(Guid orderId, string? gatewayTransactionId = null)
        {
            var order = await _billingOrderRepo.GetByIdIgnoreTenantAsync(orderId);
            if (order == null) throw new Exception("Không tìm thấy đơn thanh toán");

            var discount = order.DiscountAmount ?? 0m;
            var payable = order.TotalAmount - discount;
            if (payable <= 0m)
                throw new Exception("Đơn thanh toán không hợp lệ (số tiền phải > 0)");

            var expectedMinor = checked((long)decimal.Round(payable * 100m, 0, MidpointRounding.AwayFromZero));

            var tmnCode = _config["Payment:VNPay:TmnCode"] ?? throw new Exception("Missing config: Payment:VNPay:TmnCode");
            var hashSecret = _config["Payment:VNPay:HashSecret"] ?? throw new Exception("Missing config: Payment:VNPay:HashSecret");

            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            var payDate = vnTime.ToString("yyyyMMddHHmmss");

            var transactionNo = string.IsNullOrWhiteSpace(gatewayTransactionId)
                ? RandomNumberGenerator.GetInt32(10000000, 99999999).ToString()
                : gatewayTransactionId.Trim();

            var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = expectedMinor.ToString(CultureInfo.InvariantCulture),
                ["vnp_TxnRef"] = order.Id.ToString(),
                ["vnp_ResponseCode"] = "00",
                ["vnp_TransactionStatus"] = "00",
                ["vnp_TransactionNo"] = transactionNo,
                ["vnp_BankCode"] = "NCB",
                ["vnp_PayDate"] = payDate,
                ["vnp_OrderInfo"] = $"SIMULATE SUCCESS {order.BillingOrderNumber}",
            };

            var signData = string.Join("&", vnpParams.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));
            var secureHash = HmacSha512(signData, hashSecret);
            return signData + $"&vnp_SecureHash={secureHash}";
        }

        // Background job để active Tenant và gửi email (gọi từ Hangfire)
        public async Task ActivateTenantAfterPaymentAsync(Guid orderId, string transactionId)
        {
            string? ownerEmail = null;
            string? tenantName = null;
            bool shouldSendEmail = false;

            await _transaction.ExecuteAsync(async () =>
            {
                var order = await _billingOrderRepo.GetByIdIgnoreTenantAsync(orderId);
                if (order == null) throw new Exception("Không tìm thấy đơn thanh toán");

                if (!string.Equals(order.PaymentStatus, StatusEnum.PaymentPaid, StringComparison.OrdinalIgnoreCase))
                    return;

                var tenant = await _tenantRepo.GetByIdIgnoreTenantAsync(order.TenantId);
                if (tenant == null) throw new Exception("Không tìm thấy tenant");

                tenantName = tenant.Name;

                var canProceed = string.Equals(tenant.Status, StatusEnum.TenantActive, StringComparison.OrdinalIgnoreCase)
                                 || BillingStateMachine.CanActivateTenant(tenant.Status);
                if (!canProceed) return;

                var orderModules = await _billingOrderModuleRepo.GetByBillingOrderIdIgnoreTenantAsync(order.Id);
                if (orderModules.Count == 0)
                    throw new Exception("Đơn thanh toán không có module nào");

                var now = DateTime.UtcNow;
                DateTime maxEndDate = now;
                foreach (var line in orderModules)
                {
                    var existingSub = await _moduleSubscriptionRepo.GetByTenantAndModuleIgnoreTenantAsync(tenant.Id, line.ModuleId);
                    if (existingSub == null)
                    {
                        existingSub = new ModuleSubscription
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenant.Id,
                            ModuleId = line.ModuleId,
                            StartDate = now,
                            EndDate = now,
                            Status = StatusEnum.ModuleActive,
                            CreatedAt = now,
                            IsDeleted = false
                        };
                        await _moduleSubscriptionRepo.AddAsync(existingSub);
                    }

                    var baseDate = existingSub.EndDate > now ? existingSub.EndDate : now;
                    existingSub.EndDate = baseDate.AddMonths(1);
                    existingSub.Status = StatusEnum.ModuleActive;
                    await _moduleSubscriptionRepo.UpdateIgnoreTenantAsync(existingSub);

                    if (existingSub.EndDate > maxEndDate) 
                        maxEndDate = existingSub.EndDate;
                }

                tenant.Status = StatusEnum.TenantActive;
                tenant.SubscriptionEndDate = DateOnly.FromDateTime(maxEndDate);

                var ownerUser = tenant.OwnerUserId.HasValue ? await _userRepo.GetByIdIgnoreTenantAsync(tenant.OwnerUserId.Value) : null;
                if (ownerUser != null)
                {
                    ownerUser.IsActive = true;
                    await _userRepo.UpdateUserIgnoreTenantAsync(ownerUser);
                    ownerEmail = ownerUser.Email;
                }

                await _tenantRepo.UpdateIgnoreTenantAsync(tenant);
                shouldSendEmail = !string.IsNullOrWhiteSpace(ownerEmail);
            });

            if (shouldSendEmail && ownerEmail != null && tenantName != null)
            {
                await _emailService.SendEmailAsync(
                    ownerEmail,
                    "Thanh toán thành công - Kích hoạt tài khoản SMEFLOW",
                    $"<h3>Chúc mừng {tenantName}!</h3><p>Tài khoản của bạn đã được kích hoạt.</p><p>Mã giao dịch: {transactionId}</p><p>Bạn có thể đăng nhập ngay bây giờ.</p>"
                );
            }
        }

        private string HmacSha512(string data, string key)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Tạo URL thanh toán VNPay — đơn giản, giống reference pattern.
        /// </summary>
        private string CreateVNPayUrl(BillingOrder order, string clientIp)
        {
            var tmnCode = _config["Payment:VNPay:TmnCode"] ?? throw new Exception("Missing config: Payment:VNPay:TmnCode");
            var hashSecret = _config["Payment:VNPay:HashSecret"] ?? throw new Exception("Missing config: Payment:VNPay:HashSecret");
            var baseUrl = _config["Payment:VNPay:BaseUrl"] ?? throw new Exception("Missing config: Payment:VNPay:BaseUrl");
            var callbackUrl = _config["Payment:VNPay:CallbackUrl"] ?? throw new Exception("Missing config: Payment:VNPay:CallbackUrl");

            _vnpay.Initialize(tmnCode, hashSecret, baseUrl, callbackUrl);

            var discount = order.DiscountAmount ?? 0m;
            var payable = order.TotalAmount - discount;
            if (payable <= 0m)
                throw new Exception("Đơn thanh toán không hợp lệ (số tiền phải > 0)");

            var description = $"Thanh toan don {order.BillingOrderNumber}";
            if (description.Length > 100) description = description[..100];

            var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            var request = new PaymentRequest
            {
                PaymentId = order.Id.ToString(),
                Money = (double)payable,
                Description = description,
                IpAddress = clientIp,
                BankCode = BankCode.ANY,
                CreatedDate = vietnamTime,
                ExpireDate = vietnamTime.AddHours(24),
                Currency = Currency.VND,
                Language = DisplayLanguage.Vietnamese
            };

            return _vnpay.GetPaymentUrl(request);
        }
    }
}