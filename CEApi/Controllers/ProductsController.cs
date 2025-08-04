using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CEApi.Data;
using CEApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CEApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly MsSqlDatabaseContext _context;

        public ProductsController(MsSqlDatabaseContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        // GET: api/Products/{search}
        [HttpGet("{search}")]
        public async Task<ActionResult<Product>> GetProduct(string search)
        {
            search = search.Trim().ToLower();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductCode.ToLower() == search || p.EanCode == search);

            if (product == null)
            {
                return NotFound();
            }

            var statistics = await _context.Statistics.FirstAsync();
            if (statistics != null)
            {
                if (search.Equals(product.ProductCode, StringComparison.OrdinalIgnoreCase))
                {
                    statistics.LookupsByCode++;
                } else
                {
                    statistics.LookupsByEAN++;
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                Console.Error.WriteLine("GetProductByProductCode: Statistics not found, unable to increment statistics.");
            }

            return product;
        }

        // PUT: api/Products/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(string id, Product product)
        {
            if (id != product.ProductCode)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (ProductExists(product.ProductCode))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetProductByProductCode", new { search = product.ProductCode }, product);
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Products/Count
        [HttpGet("Count")]
        public async Task<IActionResult> GetProductCount()
        {
            var count = await _context.Products.CountAsync();
            return Ok(new {productcount = count});
        }

        private bool ProductExists(string id)
        {
            return _context.Products.Any(e => e.ProductCode == id);
        }
    }
}
