using System;
using System.Linq;
using System.Web.Http;
using GadgetHub.API.Models;

namespace GadgetHub.API.Controllers
{
    [RoutePrefix("api/dashboard")]
    public class DashboardController : ApiController
    {
        private GadgetHubDBContext db = new GadgetHubDBContext();
        
        [HttpGet]
        [Route("customer/{customerId:int}/summary")]
        public IHttpActionResult GetCustomerDashboardSummary(int customerId)
        {
            try
            {
                var currentDate = DateTime.Now;
                var thirtyDaysAgo = currentDate.AddDays(-30);
                
                // Get customer statistics
                var totalOrders = db.Orders.Count(o => o.CustomerID == customerId);
                var pendingOrders = db.Orders.Count(o => o.CustomerID == customerId && o.Status == "Pending");
                var confirmedOrders = db.Orders.Count(o => o.CustomerID == customerId && o.Status == "Confirmed");
                var deliveredOrders = db.Orders.Count(o => o.CustomerID == customerId && o.Status == "Delivered");
                var cancelledOrders = db.Orders.Count(o => o.CustomerID == customerId && o.Status == "Cancelled");
                
                var totalSpent = db.Orders
                    .Where(o => o.CustomerID == customerId && (o.Status == "Confirmed" || o.Status == "Delivered"))
                    .Sum(o => o.TotalAmount) ?? 0;
                
                var recentOrders = db.Orders
                    .Where(o => o.CustomerID == customerId && o.CreatedDate >= thirtyDaysAgo)
                    .Count();
                
                var unreadNotifications = db.Notifications
                    .Count(n => n.CustomerID == customerId && !n.IsRead);
                
                var cartItemsCount = db.Cart
                    .Where(c => c.CustomerID == customerId)
                    .Sum(c => c.Quantity);
                
                var pendingQuotations = db.Quotations
                    .Count(q => q.CustomerID == customerId && q.Status == "Pending");
                
                // Get recent activity
                var recentActivity = db.Orders
                    .Where(o => o.CustomerID == customerId)
                    .OrderByDescending(o => o.CreatedDate)
                    .Take(5)
                    .Select(o => new
                    {
                        Type = "Order",
                        Description = $"Order #{o.OrderID} - {o.Status}",
                        Date = o.CreatedDate,
                        Amount = o.TotalAmount
                    })
                    .ToList();
                
                // Get favorite categories
                var favoriteCategories = db.Orders
                    .Where(o => o.CustomerID == customerId)
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.Product.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        OrderCount = g.Count(),
                        TotalSpent = g.Sum(oi => oi.PricePerUnit * oi.Quantity)
                    })
                    .OrderByDescending(c => c.OrderCount)
                    .Take(3)
                    .ToList();
                
                return Ok(new
                {
                    CustomerID = customerId,
                    Summary = new
                    {
                        TotalOrders = totalOrders,
                        PendingOrders = pendingOrders,
                        ConfirmedOrders = confirmedOrders,
                        DeliveredOrders = deliveredOrders,
                        CancelledOrders = cancelledOrders,
                        TotalSpent = totalSpent,
                        RecentOrders = recentOrders,
                        UnreadNotifications = unreadNotifications,
                        CartItemsCount = cartItemsCount,
                        PendingQuotations = pendingQuotations
                    },
                    RecentActivity = recentActivity,
                    FavoriteCategories = favoriteCategories,
                    GeneratedAt = currentDate
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("distributor/{distributorId:int}/summary")]
        public IHttpActionResult GetDistributorDashboardSummary(int distributorId)
        {
            try
            {
                var currentDate = DateTime.Now;
                var thirtyDaysAgo = currentDate.AddDays(-30);
                
                // Get distributor statistics
                var totalOrders = db.Orders.Count(o => o.SelectedDistributorID == distributorId);
                var pendingOrders = db.Orders.Count(o => o.SelectedDistributorID == distributorId && o.Status == "Pending");
                var confirmedOrders = db.Orders.Count(o => o.SelectedDistributorID == distributorId && o.Status == "Confirmed");
                var deliveredOrders = db.Orders.Count(o => o.SelectedDistributorID == distributorId && o.Status == "Delivered");
                var cancelledOrders = db.Orders.Count(o => o.SelectedDistributorID == distributorId && o.Status == "Cancelled");
                
                var totalRevenue = db.Orders
                    .Where(o => o.SelectedDistributorID == distributorId && (o.Status == "Confirmed" || o.Status == "Delivered"))
                    .Sum(o => o.TotalAmount) ?? 0;
                
                var recentOrders = db.Orders
                    .Where(o => o.SelectedDistributorID == distributorId && o.CreatedDate >= thirtyDaysAgo)
                    .Count();
                
                var pendingQuotations = db.DistributorResponses
                    .Count(dr => dr.DistributorID == distributorId && !dr.IsSubmitted);
                
                var submittedQuotations = db.DistributorResponses
                    .Where(dr => dr.DistributorID == distributorId && dr.IsSubmitted)
                    .Select(dr => dr.QuotationID)
                    .Distinct()
                    .Count();
                
                var winRate = submittedQuotations > 0 ? 
                    (double)totalOrders / submittedQuotations * 100 : 0;
                
                // Get recent activity
                var recentActivity = db.Orders
                    .Where(o => o.SelectedDistributorID == distributorId)
                    .OrderByDescending(o => o.CreatedDate)
                    .Take(5)
                    .Select(o => new
                    {
                        Type = "Order",
                        Description = $"Order #{o.OrderID} from {o.Customer.Name}",
                        Date = o.CreatedDate,
                        Amount = o.TotalAmount,
                        Status = o.Status
                    })
                    .ToList();
                
                // Get top products
                var topProducts = db.Orders
                    .Where(o => o.SelectedDistributorID == distributorId)
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => new { oi.ProductID, oi.Product.Name })
                    .Select(g => new
                    {
                        ProductID = g.Key.ProductID,
                        ProductName = g.Key.Name,
                        OrderCount = g.Count(),
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.PricePerUnit * oi.Quantity)
                    })
                    .OrderByDescending(p => p.TotalRevenue)
                    .Take(5)
                    .ToList();
                
                // Get monthly performance
                var monthlyPerformance = db.Orders
                    .Where(o => o.SelectedDistributorID == distributorId && o.CreatedDate >= currentDate.AddMonths(-6))
                    .GroupBy(o => new { o.CreatedDate.Year, o.CreatedDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(m => m.Year)
                    .ThenBy(m => m.Month)
                    .ToList();
                
                return Ok(new
                {
                    DistributorID = distributorId,
                    Summary = new
                    {
                        TotalOrders = totalOrders,
                        PendingOrders = pendingOrders,
                        ConfirmedOrders = confirmedOrders,
                        DeliveredOrders = deliveredOrders,
                        CancelledOrders = cancelledOrders,
                        TotalRevenue = totalRevenue,
                        RecentOrders = recentOrders,
                        PendingQuotations = pendingQuotations,
                        SubmittedQuotations = submittedQuotations,
                        WinRate = Math.Round(winRate, 1)
                    },
                    RecentActivity = recentActivity,
                    TopProducts = topProducts,
                    MonthlyPerformance = monthlyPerformance,
                    GeneratedAt = currentDate
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("system/summary")]
        public IHttpActionResult GetSystemSummary()
        {
            try
            {
                var currentDate = DateTime.Now;
                var thirtyDaysAgo = currentDate.AddDays(-30);
                
                // System-wide statistics
                var totalCustomers = db.Customers.Count();
                var totalOrders = db.Orders.Count();
                var totalRevenue = db.Orders
                    .Where(o => o.Status == "Confirmed" || o.Status == "Delivered")
                    .Sum(o => o.TotalAmount) ?? 0;
                
                var recentCustomers = db.Customers
                    .Count(c => c.CreatedDate >= thirtyDaysAgo);
                
                var recentOrders = db.Orders
                    .Count(o => o.CreatedDate >= thirtyDaysAgo);
                
                var pendingQuotations = db.Quotations
                    .Count(q => q.Status == "Pending");
                
                // Distributor performance
                var distributorPerformance = db.Distributors
                    .Select(d => new
                    {
                        d.DistributorID,
                        d.Name,
                        TotalOrders = db.Orders.Count(o => o.SelectedDistributorID == d.DistributorID),
                        TotalRevenue = db.Orders
                            .Where(o => o.SelectedDistributorID == d.DistributorID && (o.Status == "Confirmed" || o.Status == "Delivered"))
                            .Sum(o => o.TotalAmount) ?? 0,
                        PendingQuotations = db.DistributorResponses
                            .Count(dr => dr.DistributorID == d.DistributorID && !dr.IsSubmitted)
                    })
                    .ToList();
                
                return Ok(new
                {
                    SystemSummary = new
                    {
                        TotalCustomers = totalCustomers,
                        TotalOrders = totalOrders,
                        TotalRevenue = totalRevenue,
                        RecentCustomers = recentCustomers,
                        RecentOrders = recentOrders,
                        PendingQuotations = pendingQuotations
                    },
                    DistributorPerformance = distributorPerformance,
                    GeneratedAt = currentDate
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
}