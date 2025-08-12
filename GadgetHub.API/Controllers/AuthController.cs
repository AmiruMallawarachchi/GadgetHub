using System;
using System.Linq;
using System.Web.Http;
using GadgetHub.API.Models;

namespace GadgetHub.API.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private GadgetHubDBContext db = new GadgetHubDBContext();
        
        [HttpPost]
        [Route("customer/login")]
        public IHttpActionResult CustomerLogin([FromBody] LoginRequest request)
        {
            try
            {
                var customer = db.Customers.FirstOrDefault(c => c.Email == request.Email && c.Password == request.Password);
                
                if (customer != null)
                {
                    return Ok(new { 
                        success = true, 
                        customerID = customer.CustomerID,
                        name = customer.Name,
                        email = customer.Email,
                        userType = "customer"
                    });
                }
                
                return Ok(new { success = false, message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPost]
        [Route("distributor/login")]
        public IHttpActionResult DistributorLogin([FromBody] LoginRequest request)
        {
            try
            {
                var distributor = db.Distributors.FirstOrDefault(d => d.Email == request.Email && d.Password == request.Password);
                
                if (distributor != null)
                {
                    return Ok(new { 
                        success = true, 
                        distributorID = distributor.DistributorID,
                        name = distributor.Name,
                        email = distributor.Email,
                        userType = "distributor"
                    });
                }
                
                return Ok(new { success = false, message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPost]
        [Route("customer/register")]
        public IHttpActionResult CustomerRegister([FromBody] CustomerRegisterRequest request)
        {
            try
            {
                // Check if email already exists
                if (db.Customers.Any(c => c.Email == request.Email))
                {
                    return Ok(new { success = false, message = "Email already exists" });
                }
                
                var customer = new Customer
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = request.Password,
                    Phone = request.Phone,
                    Address = request.Address,
                    CreatedDate = DateTime.Now
                };
                
                db.Customers.Add(customer);
                db.SaveChanges();
                
                return Ok(new { 
                    success = true, 
                    message = "Registration successful",
                    customerID = customer.CustomerID
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    
    public class CustomerRegisterRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}