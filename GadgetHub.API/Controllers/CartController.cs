using System;
using System.Linq;
using System.Web.Http;
using GadgetHub.API.Models;

namespace GadgetHub.API.Controllers
{
    [RoutePrefix("api/cart")]
    public class CartController : ApiController
    {
        private GadgetHubDBContext db = new GadgetHubDBContext();
        
        [HttpGet]
        [Route("{customerId:int}")]
        public IHttpActionResult GetCustomerCart(int customerId)
        {
            try
            {
                var cartItems = db.Cart
                    .Where(c => c.CustomerID == customerId)
                    .Select(c => new
                    {
                        c.CartID,
                        c.CustomerID,
                        c.ProductID,
                        c.Quantity,
                        c.AddedDate,
                        ProductName = c.Product.Name,
                        ProductDescription = c.Product.Description,
                        ProductCategory = c.Product.Category,
                        ProductImageURL = c.Product.ImageURL
                    }).ToList();
                
                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPost]
        [Route("add")]
        public IHttpActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // Check if item already exists in cart
                var existingItem = db.Cart.FirstOrDefault(c => 
                    c.CustomerID == request.CustomerID && c.ProductID == request.ProductID);
                
                if (existingItem != null)
                {
                    // Update quantity
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    // Add new item
                    var cartItem = new Cart
                    {
                        CustomerID = request.CustomerID,
                        ProductID = request.ProductID,
                        Quantity = request.Quantity,
                        AddedDate = DateTime.Now
                    };
                    
                    db.Cart.Add(cartItem);
                }
                
                db.SaveChanges();
                
                return Ok(new { success = true, message = "Item added to cart successfully" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Error adding to cart: " + ex.Message });
            }
        }
        
        [HttpPut]
        [Route("update")]
        public IHttpActionResult UpdateCartItem([FromBody] UpdateCartRequest request)
        {
            try
            {
                var cartItem = db.Cart.Find(request.CartID);
                
                if (cartItem == null)
                {
                    return NotFound();
                }
                
                cartItem.Quantity = request.Quantity;
                db.SaveChanges();
                
                return Ok(new { success = true, message = "Cart item updated successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpDelete]
        [Route("{cartId:int}")]
        public IHttpActionResult RemoveFromCart(int cartId)
        {
            try
            {
                var cartItem = db.Cart.Find(cartId);
                
                if (cartItem == null)
                {
                    return NotFound();
                }
                
                db.Cart.Remove(cartItem);
                db.SaveChanges();
                
                return Ok(new { success = true, message = "Item removed from cart successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpDelete]
        [Route("clear/{customerId:int}")]
        public IHttpActionResult ClearCart(int customerId)
        {
            try
            {
                var cartItems = db.Cart.Where(c => c.CustomerID == customerId).ToList();
                
                if (cartItems.Any())
                {
                    db.Cart.RemoveRange(cartItems);
                    db.SaveChanges();
                }
                
                return Ok(new { success = true, message = "Cart cleared successfully" });
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
    
    public class AddToCartRequest
    {
        public int CustomerID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
    }
    
    public class UpdateCartRequest
    {
        public int CartID { get; set; }
        public int Quantity { get; set; }
    }
}