using System;
using System.Linq;
using System.Web.Http;
using GadgetHub.API.Models;

namespace GadgetHub.API.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private GadgetHubDBContext db = new GadgetHubDBContext();
        
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllProducts()
        {
            try
            {
                var products = db.Products.Select(p => new
                {
                    p.ProductID,
                    p.Name,
                    p.Description,
                    p.ImageURL,
                    p.Category,
                    p.CreatedDate
                }).ToList();
                
                return Ok(products);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetProduct(int id)
        {
            try
            {
                var product = db.Products.Find(id);
                
                if (product == null)
                {
                    return NotFound();
                }
                
                return Ok(product);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        
        [HttpGet]
        [Route("category/{category}")]
        public IHttpActionResult GetProductsByCategory(string category)
        {
            try
            {
                var products = db.Products
                    .Where(p => p.Category.ToLower() == category.ToLower())
                    .Select(p => new
                    {
                        p.ProductID,
                        p.Name,
                        p.Description,
                        p.ImageURL,
                        p.Category,
                        p.CreatedDate
                    }).ToList();
                
                return Ok(products);
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