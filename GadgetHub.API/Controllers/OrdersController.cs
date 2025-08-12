using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using GadgetHub.API.Models;

namespace GadgetHub.API.Controllers
{
    [RoutePrefix("api/orders")]
    public class OrdersController : ApiController
    {
        private GadgetHubDBContext db = new GadgetHubDBContext();
        
        [HttpGet]
        [Route("customer/{customerId:int}")]
        public IHttpActionResult GetCustomerOrders(int customerId)
        {
            try
            {
                var orders = db.Orders
                    .Where(o => o.CustomerID == customerId)
                    .Select(o => new
                    {
                        o.OrderID,
                        o.QuotationID,
                        o.TotalAmount,
                        o.Status,
                        o.EstimatedDeliveryDate,
                        o.CreatedDate,
                        o.ConfirmedDate,
                        DistributorName = o.SelectedDistributor.Name,
                        DistributorEmail = o.SelectedDistributor.Email,
                        DistributorPhone = o.SelectedDistributor.Phone,
                        Items = db.OrderItems
                            .Where(oi => oi.OrderID == o.OrderID)
                            .Select(oi => new
                            {
                                oi.ProductID,
                                ProductName = oi.Product.Name,
                                oi.Quantity,
                                oi.PricePerUnit,
                                Total = oi.Quantity * oi.PricePerUnit
                            }).ToList()
                    })
                    .OrderByDescending(o => o.CreatedDate)
                    .ToList();
                
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("distributor/{distributorId:int}")]
        public IHttpActionResult GetDistributorOrders(int distributorId)
        {
            try
            {
                var orders = db.Orders
                    .Where(o => o.SelectedDistributorID == distributorId)
                    .Select(o => new
                    {
                        o.OrderID,
                        o.QuotationID,
                        o.TotalAmount,
                        o.Status,
                        o.EstimatedDeliveryDate,
                        o.CreatedDate,
                        o.ConfirmedDate,
                        CustomerName = o.Customer.Name,
                        CustomerEmail = o.Customer.Email,
                        CustomerPhone = o.Customer.Phone,
                        CustomerAddress = o.Customer.Address,
                        Items = db.OrderItems
                            .Where(oi => oi.OrderID == o.OrderID)
                            .Select(oi => new
                            {
                                oi.ProductID,
                                ProductName = oi.Product.Name,
                                oi.Quantity,
                                oi.PricePerUnit,
                                Total = oi.Quantity * oi.PricePerUnit
                            }).ToList()
                    })
                    .OrderByDescending(o => o.CreatedDate)
                    .ToList();
                
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPut]
        [Route("{orderId:int}/confirm")]
        public IHttpActionResult ConfirmOrder(int orderId)
        {
            try
            {
                var order = db.Orders.Find(orderId);
                
                if (order == null)
                {
                    return NotFound();
                }
                
                if (order.Status != "Pending")
                {
                    return BadRequest($"Order is already {order.Status.ToLower()}. Cannot confirm.");
                }
                
                order.Status = "Confirmed";
                order.ConfirmedDate = DateTime.Now;
                
                // Create comprehensive notification for customer
                var orderItems = db.OrderItems.Where(oi => oi.OrderID == orderId).ToList();
                var itemCount = orderItems.Sum(oi => oi.Quantity);
                var itemSummary = itemCount == 1 ? "1 item" : $"{itemCount} items";
                
                var notification = new Notification
                {
                    CustomerID = order.CustomerID,
                    OrderID = order.OrderID,
                    Message = $"🎉 Excellent! Your order #{order.OrderID} with {itemSummary} (${order.TotalAmount:F2}) has been confirmed by {order.SelectedDistributor.Name}! " +
                             $"📦 Expected delivery: {(order.EstimatedDeliveryDate != null ? order.EstimatedDeliveryDate.Value.ToString("MMM dd, yyyy") : "TBD")}. " +
                             $"📞 Distributor contact: {order.SelectedDistributor.Phone ?? order.SelectedDistributor.Email}",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };
                
                db.Notifications.Add(notification);
                
                // Log the confirmation activity
                LogOrderActivity(orderId, "Order Confirmed", $"Order confirmed by distributor {order.SelectedDistributor.Name}");
                
                db.SaveChanges();
                
                return Ok(new { 
                    success = true, 
                    message = "Order confirmed successfully",
                    orderDetails = new
                    {
                        OrderID = order.OrderID,
                        Status = order.Status,
                        ConfirmedDate = order.ConfirmedDate,
                        EstimatedDeliveryDate = order.EstimatedDeliveryDate,
                        CustomerNotified = true
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPut]
        [Route("{orderId:int}/cancel")]
        public IHttpActionResult CancelOrder(int orderId, [FromBody] CancelOrderRequest request = null)
        {
            try
            {
                var order = db.Orders.Find(orderId);
                
                if (order == null)
                {
                    return NotFound();
                }
                
                if (order.Status == "Cancelled")
                {
                    return BadRequest("Order is already cancelled.");
                }
                
                if (order.Status == "Delivered")
                {
                    return BadRequest("Cannot cancel a delivered order.");
                }
                
                order.Status = "Cancelled";
                
                // Create detailed notification for customer
                var reason = (request != null ? request.Reason : null) ?? "Distributor was unable to fulfill the order";
                var orderItems = db.OrderItems.Where(oi => oi.OrderID == orderId).ToList();
                var itemCount = orderItems.Sum(oi => oi.Quantity);
                
                var notification = new Notification
                {
                    CustomerID = order.CustomerID,
                    OrderID = order.OrderID,
                    Message = $"❌ We're sorry, but your order #{order.OrderID} with {itemCount} item{(itemCount != 1 ? "s" : "")} " +
                             $"(${order.TotalAmount:F2}) has been cancelled by {order.SelectedDistributor.Name}. " +
                             $"Reason: {reason}. " +
                             $"💡 You can try placing a new order or contact our support team for assistance. " +
                             $"📞 Support: support@gadgethub.com",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };
                
                db.Notifications.Add(notification);
                
                // Log the cancellation activity
                LogOrderActivity(orderId, "Order Cancelled", $"Order cancelled by distributor {order.SelectedDistributor.Name}. Reason: {reason}");
                
                db.SaveChanges();
                
                return Ok(new { 
                    success = true, 
                    message = "Order cancelled successfully",
                    orderDetails = new
                    {
                        OrderID = order.OrderID,
                        Status = order.Status,
                        CancelledDate = DateTime.Now,
                        Reason = reason,
                        CustomerNotified = true
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("{orderId:int}")]
        public IHttpActionResult GetOrderDetails(int orderId)
        {
            try
            {
                var order = db.Orders
                    .Where(o => o.OrderID == orderId)
                    .Select(o => new
                    {
                        o.OrderID,
                        o.QuotationID,
                        o.CustomerID,
                        o.SelectedDistributorID,
                        o.TotalAmount,
                        o.Status,
                        o.EstimatedDeliveryDate,
                        o.CreatedDate,
                        o.ConfirmedDate,
                        Customer = new
                        {
                            o.Customer.Name,
                            o.Customer.Email,
                            o.Customer.Phone,
                            o.Customer.Address
                        },
                        Distributor = new
                        {
                            o.SelectedDistributor.Name,
                            o.SelectedDistributor.Email,
                            o.SelectedDistributor.Phone,
                            o.SelectedDistributor.Address
                        },
                        Items = db.OrderItems
                            .Where(oi => oi.OrderID == o.OrderID)
                            .Select(oi => new
                            {
                                oi.ProductID,
                                ProductName = oi.Product.Name,
                                ProductDescription = oi.Product.Description,
                                oi.Quantity,
                                oi.PricePerUnit,
                                Total = oi.Quantity * oi.PricePerUnit
                            }).ToList()
                    })
                    .FirstOrDefault();
                
                if (order == null)
                {
                    return NotFound();
                }
                
                return Ok(order);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPut]
        [Route("{orderId:int}/deliver")]
        public IHttpActionResult MarkAsDelivered(int orderId)
        {
            try
            {
                var order = db.Orders.Find(orderId);
                
                if (order == null)
                {
                    return NotFound();
                }
                
                if (order.Status != "Confirmed")
                {
                    return BadRequest($"Order must be confirmed before marking as delivered. Current status: {order.Status}");
                }
                
                order.Status = "Delivered";
                
                // Create delivery notification for customer
                var orderItems = db.OrderItems.Where(oi => oi.OrderID == orderId).ToList();
                var itemCount = orderItems.Sum(oi => oi.Quantity);
                
                var notification = new Notification
                {
                    CustomerID = order.CustomerID,
                    OrderID = order.OrderID,
                    Message = $"📦✅ Great news! Your order #{order.OrderID} with {itemCount} item{(itemCount != 1 ? "s" : "")} " +
                             $"has been successfully delivered by {order.SelectedDistributor.Name}! " +
                             $"🎉 Thank you for choosing GadgetHub. We hope you enjoy your new gadgets! " +
                             $"⭐ Please consider leaving a review of your experience.",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };
                
                db.Notifications.Add(notification);
                
                // Log the delivery activity
                LogOrderActivity(orderId, "Order Delivered", $"Order delivered by distributor {order.SelectedDistributor.Name}");
                
                db.SaveChanges();
                
                return Ok(new { 
                    success = true, 
                    message = "Order marked as delivered successfully",
                    orderDetails = new
                    {
                        OrderID = order.OrderID,
                        Status = order.Status,
                        DeliveredDate = DateTime.Now,
                        CustomerNotified = true
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("{orderId:int}/status")]
        public IHttpActionResult GetOrderStatus(int orderId)
        {
            try
            {
                var order = db.Orders.Find(orderId);
                
                if (order == null)
                {
                    return NotFound();
                }
                
                return Ok(new
                {
                    OrderID = order.OrderID,
                    Status = order.Status,
                    CreatedDate = order.CreatedDate,
                    ConfirmedDate = order.ConfirmedDate,
                    EstimatedDeliveryDate = order.EstimatedDeliveryDate,
                    TotalAmount = order.TotalAmount,
                    DistributorName = order.SelectedDistributor.Name,
                    DistributorContact = order.SelectedDistributor.Phone ?? order.SelectedDistributor.Email,
                    StatusHistory = GetOrderStatusHistory(orderId)
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPost]
        [Route("{orderId:int}/notify")]
        public IHttpActionResult SendCustomNotification(int orderId, [FromBody] CustomNotificationRequest request)
        {
            try
            {
                var order = db.Orders.Find(orderId);
                
                if (order == null)
                {
                    return NotFound();
                }
                
                var notification = new Notification
                {
                    CustomerID = order.CustomerID,
                    OrderID = order.OrderID,
                    Message = $"📢 Update on Order #{order.OrderID}: {request.Message}",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };
                
                db.Notifications.Add(notification);
                
                // Log the custom notification
                LogOrderActivity(orderId, "Custom Notification", request.Message);
                
                db.SaveChanges();
                
                return Ok(new { success = true, message = "Custom notification sent successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        private void LogOrderActivity(int orderId, string activity, string details)
        {
            // This could be expanded to a proper audit log table
            // For now, we'll use a simple approach
            System.Diagnostics.Debug.WriteLine($"Order {orderId} - {activity}: {details} at {DateTime.Now}");
        }
        
        private List<object> GetOrderStatusHistory(int orderId)
        {
            // This would typically come from an audit log table
            // For now, return basic status progression
            var order = db.Orders.Find(orderId);
            var history = new List<object>();
            
            history.Add(new { Status = "Created", Date = order.CreatedDate, Description = "Order created and assigned to distributor" });
            
            if (order.ConfirmedDate.HasValue)
            {
                history.Add(new { Status = "Confirmed", Date = order.ConfirmedDate.Value, Description = "Order confirmed by distributor" });
            }
            
            if (order.Status == "Delivered")
            {
                history.Add(new { Status = "Delivered", Date = DateTime.Now, Description = "Order delivered to customer" });
            }
            else if (order.Status == "Cancelled")
            {
                history.Add(new { Status = "Cancelled", Date = DateTime.Now, Description = "Order cancelled" });
            }
            
            return history;
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
    
    public class CancelOrderRequest
    {
        public string Reason { get; set; }
    }
    
    public class CustomNotificationRequest
    {
        public string Message { get; set; }
    }
}