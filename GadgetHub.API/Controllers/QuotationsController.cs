using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using GadgetHub.API.Models;

namespace GadgetHub.API.Controllers
{
    [RoutePrefix("api/quotations")]
    public class QuotationsController : ApiController
    {
        private GadgetHubDBContext db = new GadgetHubDBContext();
        
        [HttpPost]
        [Route("create")]
        public IHttpActionResult CreateQuotation([FromBody] CreateQuotationRequest request)
        {
            try
            {
                // Create quotation
                var quotation = new Quotation
                {
                    CustomerID = request.CustomerID,
                    Status = "Pending",
                    CreatedDate = DateTime.Now
                };
                
                db.Quotations.Add(quotation);
                db.SaveChanges();
                
                // Add quotation items from cart
                var cartItems = db.Cart.Where(c => c.CustomerID == request.CustomerID).ToList();
                
                foreach (var cartItem in cartItems)
                {
                    var quotationItem = new QuotationItem
                    {
                        QuotationID = quotation.QuotationID,
                        ProductID = cartItem.ProductID,
                        Quantity = cartItem.Quantity
                    };
                    
                    db.QuotationItems.Add(quotationItem);
                }
                
                // Create distributor response entries for all 3 distributors
                var distributors = db.Distributors.ToList();
                
                foreach (var cartItem in cartItems)
                {
                    foreach (var distributor in distributors)
                    {
                        var response = new DistributorResponse
                        {
                            QuotationID = quotation.QuotationID,
                            DistributorID = distributor.DistributorID,
                            ProductID = cartItem.ProductID,
                            IsSubmitted = false
                        };
                        
                        db.DistributorResponses.Add(response);
                    }
                }
                
                db.SaveChanges();
                
                // Clear customer's cart
                db.Cart.RemoveRange(cartItems);
                db.SaveChanges();
                
                return Ok(new { 
                    success = true, 
                    quotationID = quotation.QuotationID,
                    message = "Quotation created successfully and sent to distributors" 
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("customer/{customerId:int}")]
        public IHttpActionResult GetCustomerQuotations(int customerId)
        {
            try
            {
                var totalDistributors = db.Distributors.Count();
                
                var quotations = db.Quotations
                    .Where(q => q.CustomerID == customerId)
                    .Select(q => new
                    {
                        q.QuotationID,
                        q.CustomerID,
                        q.Status,
                        q.CreatedDate,
                        TotalDistributors = totalDistributors,
                        RespondedDistributors = db.DistributorResponses
                            .Where(dr => dr.QuotationID == q.QuotationID && dr.IsSubmitted == true)
                            .Select(dr => dr.DistributorID)
                            .Distinct()
                            .Count(),
                        AllDistributorsResponded = db.DistributorResponses
                            .Where(dr => dr.QuotationID == q.QuotationID && dr.IsSubmitted == true)
                            .Select(dr => dr.DistributorID)
                            .Distinct()
                            .Count() == totalDistributors,
                        Items = db.QuotationItems
                            .Where(qi => qi.QuotationID == q.QuotationID)
                            .Select(qi => new
                            {
                                qi.QuotationItemID,
                                qi.ProductID,
                                ProductName = qi.Product.Name,
                                RequiredQuantity = qi.Quantity,
                                // Get final price if order was created
                                PricePerUnit = db.Orders
                                    .Where(o => o.QuotationID == q.QuotationID)
                                    .SelectMany(o => o.OrderItems)
                                    .Where(oi => oi.ProductID == qi.ProductID)
                                    .Select(oi => oi.PricePerUnit)
                                    .FirstOrDefault()
                            }).ToList()
                    })
                    .OrderByDescending(q => q.CreatedDate)
                    .ToList();
                
                return Ok(quotations);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("distributor/{distributorId:int}")]
        public IHttpActionResult GetDistributorQuotations(int distributorId)
        {
            try
            {
                var quotations = db.DistributorResponses
                    .Where(dr => dr.DistributorID == distributorId)
                    .GroupBy(dr => dr.QuotationID)
                    .Select(g => new
                    {
                        QuotationID = g.Key,
                        CustomerName = g.FirstOrDefault().Quotation.Customer.Name,
                        CustomerEmail = g.FirstOrDefault().Quotation.Customer.Email,
                        CreatedDate = g.FirstOrDefault().Quotation.CreatedDate,
                        Status = g.FirstOrDefault().Quotation.Status,
                        IsSubmitted = g.All(dr => dr.IsSubmitted),
                        Items = g.Select(dr => new
                        {
                            dr.ResponseID,
                            dr.ProductID,
                            ProductName = dr.Product.Name,
                            RequiredQuantity = db.QuotationItems
                                .Where(qi => qi.QuotationID == dr.QuotationID && qi.ProductID == dr.ProductID)
                                .Select(qi => qi.Quantity)
                                .FirstOrDefault(),
                            dr.PricePerUnit,
                            dr.AvailableQuantity,
                            dr.DeliveryDays,
                            dr.IsSubmitted
                        }).ToList()
                    }).ToList();
                
                return Ok(quotations);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPost]
        [Route("respond")]
        public IHttpActionResult SubmitDistributorResponse([FromBody] DistributorResponseRequest request)
        {
            try
            {
                foreach (var item in request.Items)
                {
                    var response = db.DistributorResponses.Find(item.ResponseID);
                    if (response != null)
                    {
                        response.PricePerUnit = item.PricePerUnit;
                        response.AvailableQuantity = item.AvailableQuantity;
                        response.DeliveryDays = item.DeliveryDays;
                        response.IsSubmitted = true;
                        response.SubmittedDate = DateTime.Now;
                    }
                }
                
                db.SaveChanges();
                
                // Check if all distributors have responded
                var quotationId = request.Items.First().QuotationID;
                var allResponses = db.DistributorResponses
                    .Where(dr => dr.QuotationID == quotationId)
                    .ToList();
                
                if (allResponses.All(dr => dr.IsSubmitted))
                {
                    // All distributors have responded, select best distributor
                    SelectBestDistributor(quotationId);
                }
                
                return Ok(new { success = true, message = "Response submitted successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        private double CalculateDistributorScore(decimal totalPrice, int maxDeliveryDays, double stockCoveragePercentage, 
            object allDistributors)
        {
            // Multi-criteria scoring algorithm
            // Lower score is better
            
            var distributors = (IEnumerable<dynamic>)allDistributors;
            
            // Price weight: 60% (most important factor)
            var minPrice = distributors.Min(d => d.TotalPrice);
            var maxPrice = distributors.Max(d => d.TotalPrice);
            var priceRange = maxPrice - minPrice;
            var normalizedPriceScore = priceRange > 0 ? (double)(totalPrice - minPrice) / (double)priceRange : 0;
            var priceScore = normalizedPriceScore * 0.6;
            
            // Delivery time weight: 25%
            var minDelivery = distributors.Min(d => d.MaxDeliveryDays);
            var maxDelivery = distributors.Max(d => d.MaxDeliveryDays);
            var deliveryRange = maxDelivery - minDelivery;
            var normalizedDeliveryScore = deliveryRange > 0 ? (double)(maxDeliveryDays - minDelivery) / deliveryRange : 0;
            var deliveryScore = normalizedDeliveryScore * 0.25;
            
            // Stock coverage weight: 15% (higher coverage is better, so invert the score)
            var maxStockCoverage = distributors.Max(d => d.StockCoveragePercentage);
            var minStockCoverage = distributors.Min(d => d.StockCoveragePercentage);
            var stockRange = maxStockCoverage - minStockCoverage;
            var normalizedStockScore = stockRange > 0 ? (maxStockCoverage - stockCoveragePercentage) / stockRange : 0;
            var stockScore = normalizedStockScore * 0.15;
            
            // Composite score (0 = best, 1 = worst)
            return priceScore + deliveryScore + stockScore;
        }
        
        private void SelectBestDistributor(int quotationId)
        {
            try
            {
                var quotation = db.Quotations.Find(quotationId);
                var responses = db.DistributorResponses
                    .Where(dr => dr.QuotationID == quotationId)
                    .ToList();
                
                var quotationItems = db.QuotationItems
                    .Where(qi => qi.QuotationID == quotationId)
                    .ToList();
                
                // Enhanced distributor evaluation with scoring algorithm
                var distributorScores = responses
                    .GroupBy(r => r.DistributorID)
                    .Select(g => new
                    {
                        DistributorID = g.Key,
                        DistributorName = g.First().Distributor.Name,
                        
                        // Check if distributor can fulfill entire order
                        CanFulfillOrder = quotationItems.All(qi => 
                            g.Any(r => r.ProductID == qi.ProductID && 
                                      r.AvailableQuantity >= qi.Quantity)),
                        
                        // Calculate total price for the order
                        TotalPrice = quotationItems.Sum(qi =>
                            g.Where(r => r.ProductID == qi.ProductID)
                             .Select(r => (r.PricePerUnit ?? decimal.MaxValue) * qi.Quantity)
                             .FirstOrDefault()),
                        
                        // Get maximum delivery days (worst case scenario)
                        MaxDeliveryDays = g.Max(r => r.DeliveryDays ?? int.MaxValue),
                        
                        // Calculate average delivery days for better estimation
                        AvgDeliveryDays = g.Where(r => r.DeliveryDays.HasValue)
                                          .Average(r => r.DeliveryDays.Value),
                        
                        // Check stock coverage percentage
                        StockCoveragePercentage = quotationItems.Count > 0 ? 
                            (double)quotationItems.Count(qi => 
                                g.Any(r => r.ProductID == qi.ProductID && 
                                          r.AvailableQuantity >= qi.Quantity * 2)) / quotationItems.Count * 100 : 0,
                        
                        // Individual product details for detailed analysis
                        ProductDetails = quotationItems.Select(qi => new
                        {
                            ProductID = qi.ProductID,
                            RequiredQuantity = qi.Quantity,
                            OfferedPrice = g.Where(r => r.ProductID == qi.ProductID)
                                           .Select(r => r.PricePerUnit ?? decimal.MaxValue)
                                           .FirstOrDefault(),
                            AvailableQuantity = g.Where(r => r.ProductID == qi.ProductID)
                                               .Select(r => r.AvailableQuantity ?? 0)
                                               .FirstOrDefault(),
                            DeliveryDays = g.Where(r => r.ProductID == qi.ProductID)
                                          .Select(r => r.DeliveryDays ?? int.MaxValue)
                                          .FirstOrDefault()
                        }).ToList()
                    })
                    .Where(d => d.CanFulfillOrder && d.TotalPrice < decimal.MaxValue)
                    .ToList();
                
                if (distributorScores.Any())
                {
                    // Advanced scoring algorithm - Multi-criteria decision making
                    var scoredDistributors = distributorScores.Select(d => new
                    {
                        d.DistributorID,
                        d.DistributorName,
                        d.TotalPrice,
                        d.MaxDeliveryDays,
                        d.AvgDeliveryDays,
                        d.StockCoveragePercentage,
                        d.ProductDetails,
                        
                        // Composite score calculation (lower is better)
                        CompositeScore = CalculateDistributorScore(d.TotalPrice, d.MaxDeliveryDays, d.StockCoveragePercentage, distributorScores)
                    }).ToList();
                    
                    // Select best distributor based on composite score
                    var bestDistributor = scoredDistributors
                        .OrderBy(d => d.CompositeScore)
                        .ThenBy(d => d.TotalPrice)
                        .ThenBy(d => d.MaxDeliveryDays)
                        .First();
                    
                    // Create order
                    var order = new Order
                    {
                        QuotationID = quotationId,
                        CustomerID = quotation.CustomerID,
                        SelectedDistributorID = bestDistributor.DistributorID,
                        TotalAmount = bestDistributor.TotalPrice,
                        Status = "Pending",
                        EstimatedDeliveryDate = DateTime.Now.AddDays(bestDistributor.MaxDeliveryDays),
                        CreatedDate = DateTime.Now
                    };
                    
                    db.Orders.Add(order);
                    db.SaveChanges(); // Save to get the OrderID
                    
                    // Add order items
                    foreach (var qi in quotationItems)
                    {
                        var response = responses.First(r => 
                            r.DistributorID == bestDistributor.DistributorID && 
                            r.ProductID == qi.ProductID);
                        
                        var orderItem = new OrderItem
                        {
                            OrderID = order.OrderID,
                            ProductID = qi.ProductID,
                            Quantity = qi.Quantity,
                            PricePerUnit = response.PricePerUnit
                        };
                        
                        db.OrderItems.Add(orderItem);
                    }
                    
                    // Update quotation status
                    quotation.Status = "Completed";
                    
                    // Create enhanced notification for customer
                    var distributor = db.Distributors.Find(bestDistributor.DistributorID);
                    var stockCoverageText = bestDistributor.StockCoveragePercentage > 50 ? " with excellent stock availability" : "";
                    var deliveryText = bestDistributor.MaxDeliveryDays <= 3 ? " and fast delivery" : 
                                      bestDistributor.MaxDeliveryDays <= 7 ? " and standard delivery" : " and extended delivery";
                    
                    var notification = new Notification
                    {
                        CustomerID = quotation.CustomerID,
                        OrderID = order.OrderID,
                        Message = $"🎉 Great news! We've selected {distributor.Name} as your best distributor based on competitive pricing (${bestDistributor.TotalPrice:F2}){stockCoverageText}{deliveryText} ({bestDistributor.MaxDeliveryDays} days). Your order is now pending distributor confirmation.",
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    
                    db.Notifications.Add(notification);
                    db.SaveChanges();
                }
                else
                {
                    // No distributor can fulfill the order
                    quotation.Status = "Cancelled";
                    
                    var notification = new Notification
                    {
                        CustomerID = quotation.CustomerID,
                        Message = "Sorry, none of our distributors can fulfill your order at this time. Please try again later or modify your order.",
                        IsRead = false,
                        CreatedDate = DateTime.Now
                    };
                    
                    db.Notifications.Add(notification);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Log error
                throw ex;
            }
        }
        
        [HttpGet]
        [Route("{quotationId:int}/comparison")]
        public IHttpActionResult GetQuotationComparison(int quotationId)
        {
            try
            {
                var responses = db.DistributorResponses
                    .Where(dr => dr.QuotationID == quotationId && dr.IsSubmitted)
                    .ToList();
                
                var quotationItems = db.QuotationItems
                    .Where(qi => qi.QuotationID == quotationId)
                    .ToList();
                
                if (!responses.Any())
                {
                    return Ok(new { message = "No distributor responses available yet." });
                }
                
                var comparison = responses
                    .GroupBy(r => r.DistributorID)
                    .Select(g => new
                    {
                        DistributorID = g.Key,
                        DistributorName = g.First().Distributor.Name,
                        DistributorEmail = g.First().Distributor.Email,
                        TotalPrice = quotationItems.Sum(qi =>
                            g.Where(r => r.ProductID == qi.ProductID)
                             .Select(r => (r.PricePerUnit ?? 0) * qi.Quantity)
                             .FirstOrDefault()),
                        MaxDeliveryDays = g.Max(r => r.DeliveryDays ?? 0),
                        CanFulfillOrder = quotationItems.All(qi => 
                            g.Any(r => r.ProductID == qi.ProductID && 
                                      r.AvailableQuantity >= qi.Quantity)),
                        ProductBreakdown = quotationItems.Select(qi => new
                        {
                            ProductID = qi.ProductID,
                            ProductName = qi.Product.Name,
                            RequiredQuantity = qi.Quantity,
                            OfferedPrice = g.Where(r => r.ProductID == qi.ProductID)
                                           .Select(r => r.PricePerUnit)
                                           .FirstOrDefault(),
                            AvailableQuantity = g.Where(r => r.ProductID == qi.ProductID)
                                               .Select(r => r.AvailableQuantity)
                                               .FirstOrDefault(),
                            DeliveryDays = g.Where(r => r.ProductID == qi.ProductID)
                                          .Select(r => r.DeliveryDays)
                                          .FirstOrDefault(),
                            LineTotal = g.Where(r => r.ProductID == qi.ProductID)
                                        .Select(r => (r.PricePerUnit ?? 0) * qi.Quantity)
                                        .FirstOrDefault()
                        }).ToList()
                    })
                    .OrderBy(d => d.TotalPrice)
                    .ToList();
                
                return Ok(new
                {
                    QuotationID = quotationId,
                    ComparisonDate = DateTime.Now,
                    TotalDistributors = comparison.Count,
                    QualifiedDistributors = comparison.Count(d => d.CanFulfillOrder),
                    BestPrice = comparison.Where(d => d.CanFulfillOrder).Min(d => d.TotalPrice),
                    FastestDelivery = comparison.Where(d => d.CanFulfillOrder).Min(d => d.MaxDeliveryDays),
                    Distributors = comparison
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpPost]
        [Route("{quotationId:int}/finalize")]
        public IHttpActionResult FinalizeQuotation(int quotationId)
        {
            try
            {
                var quotation = db.Quotations.Find(quotationId);
                
                if (quotation == null)
                {
                    return NotFound();
                }
                
                if (quotation.Status != "Pending")
                {
                    return Ok(new { success = false, message = "Quotation is already " + quotation.Status.ToLower() });
                }
                
                // Check if all distributors have responded
                var totalDistributors = db.Distributors.Count();
                var respondedDistributors = db.DistributorResponses
                    .Where(dr => dr.QuotationID == quotationId && dr.IsSubmitted == true)
                    .Select(dr => dr.DistributorID)
                    .Distinct()
                    .Count();
                
                if (respondedDistributors < totalDistributors)
                {
                    return Ok(new { 
                        success = false, 
                        message = $"Cannot finalize quotation. Only {respondedDistributors} out of {totalDistributors} distributors have responded." 
                    });
                }
                
                // Check if order already exists
                var existingOrder = db.Orders.FirstOrDefault(o => o.QuotationID == quotationId);
                if (existingOrder != null)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Order already exists",
                        orderID = existingOrder.OrderID,
                        distributorName = existingOrder.SelectedDistributor.Name
                    });
                }
                
                // Create the order using the existing SelectBestDistributor logic
                SelectBestDistributor(quotationId);
                
                // Get the created order
                var newOrder = db.Orders.FirstOrDefault(o => o.QuotationID == quotationId);
                
                if (newOrder != null)
                {
                    return Ok(new { 
                        success = true, 
                        message = "Order placed successfully! The best distributor has been selected.",
                        orderID = newOrder.OrderID,
                        distributorName = newOrder.SelectedDistributor.Name,
                        totalAmount = newOrder.TotalAmount,
                        estimatedDeliveryDate = newOrder.EstimatedDeliveryDate
                    });
                }
                else
                {
                    return Ok(new { 
                        success = false, 
                        message = "No distributors could fulfill your order. Please try again later." 
                    });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Error processing order: " + ex.Message });
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
    
    public class CreateQuotationRequest
    {
        public int CustomerID { get; set; }
    }
    
    public class DistributorResponseRequest
    {
        public DistributorResponseItem[] Items { get; set; }
    }
    
    public class DistributorResponseItem
    {
        public int ResponseID { get; set; }
        public int QuotationID { get; set; }
        public decimal PricePerUnit { get; set; }
        public int AvailableQuantity { get; set; }
        public int DeliveryDays { get; set; }
    }
}