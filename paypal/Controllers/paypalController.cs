using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace paypal.Controllers
{
    public class paypalController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<paypalController> _logger;

        public paypalController(IConfiguration configuration, ApplicationDbContext dbContext, ILogger<paypalController> logger)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Success()
        {
            return View();
        }
        public IActionResult Cancel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(decimal payment)
        {
            try
            {
                var environment = new SandboxEnvironment(_configuration["PayPal:ClientId"], _configuration["PayPal:ClientSecret"]);
                var client = new PayPalHttpClient(environment);

                var request = new OrdersCreateRequest();
                request.Prefer("return=representation");
                request.RequestBody(new OrderRequest
                {
                    CheckoutPaymentIntent = "CAPTURE", // Updated: Use 'CheckoutPaymentIntent'
                    PurchaseUnits = new List<PurchaseUnitRequest>
                    {
                        new PurchaseUnitRequest
                        {
                            AmountWithBreakdown = new AmountWithBreakdown
                            {
                                CurrencyCode = "USD",
                                Value = payment.ToString("0.00")
                            }
                        }
                    },
                    ApplicationContext = new ApplicationContext
                    {
                        BrandName = "Payapl Integration Syatem",
                        ReturnUrl = "https://localhost:7077/paypal/Success", // Set your return URL here
                        CancelUrl = "https://localhost:7077/paypal/Cancel" // Set your cancel URL here
                    }
                });

                var response = await client.Execute(request);

                if (response.StatusCode == HttpStatusCode.Created) // Updated: Compare to HttpStatusCode enum
                {

                    var order = response.Result<Order>();

                    // Use the logger to log the order object
                    //_logger.LogInformation("PayPal Order Response: {OrderJson}", JsonConvert.SerializeObject(order, Formatting.Indented));
                    var approvalUrl = order.Links.First(link => link.Rel == "approve").Href;
                    if (decimal.TryParse(order.PurchaseUnits[0].AmountWithBreakdown.Value, out decimal paypalAmount) && payment == paypalAmount)
                    {
                        // Store payment record in the database, even if order.Payer is null
                        var paymentRecord = new PaymentRecord
                        {
                            Amount = payment,
                            PaymentId = order.Id,
                            PayerId = order.PurchaseUnits[0].Payee.MerchantId, // Use null conditional operator to handle null case
                            Email = order.PurchaseUnits[0].Payee.Email,     // Use null conditional operator to handle null case
                            PaymentStatus = order.Status
                        };

                        _dbContext.PaymentRecords.Add(paymentRecord);
                        await _dbContext.SaveChangesAsync();

                        return Redirect(approvalUrl);
                    }
                    else
                    {
                        // Redirect to the "Cancel" action within the "Paypal" controller
                        return RedirectToAction("Cancel", "Paypal");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Error creating PayPal order.";
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;
            }

            return RedirectToAction("Index");
        }
    }
}