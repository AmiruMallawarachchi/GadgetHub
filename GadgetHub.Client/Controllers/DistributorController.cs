using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using GadgetHub.Client.Models;
using Newtonsoft.Json;

namespace GadgetHub.Client.Controllers
{
    [Authorize]
    public class DistributorController : Controller
    {
        private readonly string apiBaseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"];
        
        public async Task<ActionResult> Dashboard()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "distributor")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                var distributorId = (int)Session["DistributorID"];
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "dashboard/distributor/" + distributorId + "/summary");
                    var result = await response.Content.ReadAsStringAsync();
                    var dashboardData = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    ViewBag.DashboardData = dashboardData;
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading dashboard data: " + ex.Message;
                return View();
            }
        }
        
        public async Task<ActionResult> Quotations()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "distributor")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                var distributorId = (int)Session["DistributorID"];
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "quotations/distributor/" + distributorId);
                    var result = await response.Content.ReadAsStringAsync();
                    var quotations = JsonConvert.DeserializeObject<List<QuotationViewModel>>(result);
                    
                    return View(quotations);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading quotations: " + ex.Message;
                return View(new List<QuotationViewModel>());
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> SubmitQuotationResponse(List<QuotationItemViewModel> items)
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "distributor")
            {
                return Json(new { success = false, message = "Not authenticated" });
            }
            
            try
            {
                var requestData = new
                {
                    Items = items.Select(item => new
                    {
                        ResponseID = item.ResponseID,
                        QuotationID = item.ResponseID, // Will be set properly in API
                        PricePerUnit = item.PricePerUnit,
                        AvailableQuantity = item.AvailableQuantity,
                        DeliveryDays = item.DeliveryDays
                    }).ToArray()
                };
                
                using (var client = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await client.PostAsync(apiBaseUrl + "quotations/respond", content);
                    var result = await response.Content.ReadAsStringAsync();
                    var submitResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(submitResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error submitting response: " + ex.Message });
            }
        }
        
        public async Task<ActionResult> Orders()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "distributor")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                var distributorId = (int)Session["DistributorID"];
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "orders/distributor/" + distributorId);
                    var result = await response.Content.ReadAsStringAsync();
                    var orders = JsonConvert.DeserializeObject<List<DistributorOrderViewModel>>(result);
                    
                    return View(orders);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading orders: " + ex.Message;
                return View(new List<DistributorOrderViewModel>());
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> ConfirmOrder(int orderId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.PutAsync(apiBaseUrl + "orders/" + orderId + "/confirm", null);
                    var result = await response.Content.ReadAsStringAsync();
                    var confirmResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(confirmResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error confirming order: " + ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> CancelOrder(CancelOrderRequest request)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(new { Reason = request.Reason });
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await client.PutAsync(apiBaseUrl + "orders/" + request.OrderId + "/cancel", content);
                    var result = await response.Content.ReadAsStringAsync();
                    var cancelResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(cancelResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error cancelling order: " + ex.Message });
            }
        }
    }
}