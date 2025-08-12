using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using GadgetHub.Client.Models;
using Newtonsoft.Json;

namespace GadgetHub.Client.Controllers
{
    public class AccountController : Controller
    {
        private readonly string apiBaseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"];
        
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var json = JsonConvert.SerializeObject(model);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        
                        string endpoint = model.UserType == "distributor" 
                            ? "auth/distributor/login" 
                            : "auth/customer/login";
                        
                        var response = await client.PostAsync(apiBaseUrl + endpoint, content);
                        var result = await response.Content.ReadAsStringAsync();
                        var loginResult = JsonConvert.DeserializeObject<dynamic>(result);
                        
                        if (loginResult.success == true)
                        {
                            // Set authentication cookie
                            FormsAuthentication.SetAuthCookie(model.Email, false);
                            
                            // Set session variables
                            Session["UserType"] = model.UserType;
                            Session["UserEmail"] = model.Email;
                            
                            if (model.UserType == "customer")
                            {
                                Session["CustomerID"] = (int)loginResult.customerID;
                                Session["CustomerName"] = (string)loginResult.name;
                                return RedirectToAction("Dashboard", "Customer");
                            }
                            else
                            {
                                Session["DistributorID"] = (int)loginResult.distributorID;
                                Session["DistributorName"] = (string)loginResult.name;
                                return RedirectToAction("Dashboard", "Distributor");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", loginResult.message.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred during login: " + ex.Message);
                }
            }
            
            return View(model);
        }
        
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var json = JsonConvert.SerializeObject(model);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        
                        // Debug: Log the URL being called
                        System.Diagnostics.Debug.WriteLine($"Attempting to call: {apiBaseUrl}auth/customer/register");
                        
                        var response = await client.PostAsync(apiBaseUrl + "auth/customer/register", content);
                        var result = await response.Content.ReadAsStringAsync();
                        
                        if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(result))
                        {
                            var registerResult = JsonConvert.DeserializeObject<dynamic>(result);
                            
                            if (registerResult != null && registerResult.success == true)
                            {
                                TempData["SuccessMessage"] = "Registration successful! Please login.";
                                return RedirectToAction("Login");
                            }
                            else
                            {
                                var message = (registerResult != null && registerResult.message != null) ? registerResult.message.ToString() : "Registration failed";
                                ModelState.AddModelError("", message);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "Unable to connect to registration service. Please try again.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred during registration: " + ex.Message);
                }
            }
            
            return View(model);
        }
        
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}