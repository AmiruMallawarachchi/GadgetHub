using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using GadgetHub.Client.Models;
using Newtonsoft.Json;

namespace GadgetHub.Client.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly string apiBaseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"];
        
        public async Task<ActionResult> Dashboard()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                var customerId = (int)Session["CustomerID"];
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "dashboard/customer/" + customerId + "/summary");
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
        
        public async Task<ActionResult> Products()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "products");
                    var result = await response.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<ProductViewModel>>(result);
                    
                    return View(products);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading products: " + ex.Message;
                return View(new List<ProductViewModel>());
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return Json(new { success = false, message = "Not authenticated" });
            }
            
            try
            {
                var customerId = (int)Session["CustomerID"];
                var requestData = new { CustomerID = customerId, ProductID = productId, Quantity = quantity };
                
                using (var client = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await client.PostAsync(apiBaseUrl + "cart/add", content);
                    var result = await response.Content.ReadAsStringAsync();
                    var addResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(addResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error adding to cart: " + ex.Message });
            }
        }
        
        public async Task<ActionResult> Cart()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                var customerId = (int)Session["CustomerID"];
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "cart/" + customerId);
                    var result = await response.Content.ReadAsStringAsync();
                    var cartItems = JsonConvert.DeserializeObject<List<CartItemViewModel>>(result);
                    
                    return View(cartItems);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading cart: " + ex.Message;
                return View(new List<CartItemViewModel>());
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> UpdateCartItem(int cartId, int quantity)
        {
            try
            {
                var requestData = new { CartID = cartId, Quantity = quantity };
                
                using (var client = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await client.PutAsync(apiBaseUrl + "cart/update", content);
                    var result = await response.Content.ReadAsStringAsync();
                    var updateResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(updateResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating cart: " + ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> RemoveFromCart(int cartId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.DeleteAsync(apiBaseUrl + "cart/" + cartId);
                    var result = await response.Content.ReadAsStringAsync();
                    var removeResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(removeResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error removing from cart: " + ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> Checkout()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return Json(new { success = false, message = "Not authenticated" });
            }
            
            try
            {
                var customerId = (int)Session["CustomerID"];
                var requestData = new { CustomerID = customerId };
                
                using (var client = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await client.PostAsync(apiBaseUrl + "quotations/create", content);
                    var result = await response.Content.ReadAsStringAsync();
                    var checkoutResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(checkoutResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error during checkout: " + ex.Message });
            }
        }
        
        public async Task<ActionResult> MyOrders()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                var customerId = (int)Session["CustomerID"];
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "orders/customer/" + customerId);
                    var result = await response.Content.ReadAsStringAsync();
                    var orders = JsonConvert.DeserializeObject<List<OrderViewModel>>(result);
                    
                    return View(orders);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading orders: " + ex.Message;
                return View(new List<OrderViewModel>());
            }
        }
        
        public async Task<ActionResult> Quotations()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                var customerId = (int)Session["CustomerID"];
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "quotations/customer/" + customerId);
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
        
        public async Task<ActionResult> Notifications()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                var customerId = (int)Session["CustomerID"];
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "notifications/customer/" + customerId);
                    var result = await response.Content.ReadAsStringAsync();
                    var notifications = JsonConvert.DeserializeObject<List<NotificationViewModel>>(result);
                    
                    return View(notifications);
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading notifications: " + ex.Message;
                return View(new List<NotificationViewModel>());
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> PlaceOrder(int quotationId)
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return Json(new { success = false, message = "Not authenticated" });
            }
            
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync(apiBaseUrl + "quotations/" + quotationId + "/finalize", null);
                    var result = await response.Content.ReadAsStringAsync();
                    var finalizeResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(finalizeResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error placing order: " + ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> MarkNotificationAsRead(int notificationId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.PutAsync(apiBaseUrl + "notifications/" + notificationId + "/read", null);
                    var result = await response.Content.ReadAsStringAsync();
                    var markResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(markResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error marking notification as read: " + ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> MarkAllNotificationsAsRead()
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return Json(new { success = false, message = "Not authenticated" });
            }
            
            try
            {
                var customerId = (int)Session["CustomerID"];
                
                using (var client = new HttpClient())
                {
                    var response = await client.PutAsync(apiBaseUrl + "notifications/customer/" + customerId + "/mark-all-read", null);
                    var result = await response.Content.ReadAsStringAsync();
                    var markResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(markResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error marking all notifications as read: " + ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<ActionResult> DeleteNotification(int notificationId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.DeleteAsync(apiBaseUrl + "notifications/" + notificationId);
                    var result = await response.Content.ReadAsStringAsync();
                    var deleteResult = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    return Json(deleteResult);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting notification: " + ex.Message });
            }
        }
        
        public async Task<ActionResult> QuotationComparison(int quotationId)
        {
            if (Session["UserType"] == null || Session["UserType"].ToString() != "customer")
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(apiBaseUrl + "quotations/" + quotationId + "/comparison");
                    var result = await response.Content.ReadAsStringAsync();
                    var comparison = JsonConvert.DeserializeObject<dynamic>(result);
                    
                    ViewBag.Comparison = comparison;
                    ViewBag.QuotationId = quotationId;
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading comparison: " + ex.Message;
                return View();
            }
        }
    }
}