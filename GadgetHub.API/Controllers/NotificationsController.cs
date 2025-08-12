using System;
using System.Linq;
using System.Web.Http;
using GadgetHub.API.Models;

namespace GadgetHub.API.Controllers
{
    [RoutePrefix("api/notifications")]
    public class NotificationsController : ApiController
    {
        private GadgetHubDBContext db = new GadgetHubDBContext();
        
        [HttpGet]
        [Route("customer/{customerId:int}")]
        public IHttpActionResult GetCustomerNotifications(int customerId)
        {
            try
            {
                var notifications = db.Notifications
                    .Where(n => n.CustomerID == customerId)
                    .Select(n => new
                    {
                        n.NotificationID,
                        n.CustomerID,
                        n.OrderID,
                        n.Message,
                        n.IsRead,
                        n.CreatedDate
                    })
                    .OrderByDescending(n => n.CreatedDate)
                    .ToList();
                
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPut]
        [Route("{notificationId:int}/read")]
        public IHttpActionResult MarkAsRead(int notificationId)
        {
            try
            {
                var notification = db.Notifications.Find(notificationId);
                
                if (notification == null)
                {
                    return NotFound();
                }
                
                notification.IsRead = true;
                db.SaveChanges();
                
                return Ok(new { success = true, message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("customer/{customerId:int}/unread-count")]
        public IHttpActionResult GetUnreadCount(int customerId)
        {
            try
            {
                var count = db.Notifications
                    .Where(n => n.CustomerID == customerId && !n.IsRead)
                    .Count();
                
                return Ok(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPut]
        [Route("customer/{customerId:int}/mark-all-read")]
        public IHttpActionResult MarkAllAsRead(int customerId)
        {
            try
            {
                var notifications = db.Notifications
                    .Where(n => n.CustomerID == customerId && !n.IsRead)
                    .ToList();
                
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }
                
                db.SaveChanges();
                
                return Ok(new { 
                    success = true, 
                    message = $"Marked {notifications.Count} notifications as read",
                    markedCount = notifications.Count
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpDelete]
        [Route("{notificationId:int}")]
        public IHttpActionResult DeleteNotification(int notificationId)
        {
            try
            {
                var notification = db.Notifications.Find(notificationId);
                
                if (notification == null)
                {
                    return NotFound();
                }
                
                db.Notifications.Remove(notification);
                db.SaveChanges();
                
                return Ok(new { success = true, message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("customer/{customerId:int}/recent")]
        public IHttpActionResult GetRecentNotifications(int customerId, int count = 5)
        {
            try
            {
                var notifications = db.Notifications
                    .Where(n => n.CustomerID == customerId)
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(count)
                    .Select(n => new
                    {
                        n.NotificationID,
                        n.CustomerID,
                        n.OrderID,
                        n.Message,
                        n.IsRead,
                        n.CreatedDate,
                        TimeAgo = GetTimeAgo(n.CreatedDate)
                    })
                    .ToList();
                
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPost]
        [Route("send-system-notification")]
        public IHttpActionResult SendSystemNotification([FromBody] SystemNotificationRequest request)
        {
            try
            {
                var notification = new Notification
                {
                    CustomerID = request.CustomerID,
                    OrderID = request.OrderID,
                    Message = $"🔔 System Notification: {request.Message}",
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };
                
                db.Notifications.Add(notification);
                db.SaveChanges();
                
                return Ok(new { success = true, message = "System notification sent successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes != 1 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
            
            return dateTime.ToString("MMM dd, yyyy");
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
    
    public class SystemNotificationRequest
    {
        public int CustomerID { get; set; }
        public int? OrderID { get; set; }
        public string Message { get; set; }
    }
}