using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceListsController : ControllerBase
    {
        private readonly PriceToolContext _context;

        public PriceListsController(PriceToolContext context)
        {
            _context = context;
        }

        // GET: api/pricelists/price?clientId=GUID&productId=GUID
        [HttpGet("price")]
        public async Task<ActionResult<object>> GetPrice(Guid clientId, Guid productId)
        {
            // Get the client with price list
            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null)
            {
                return NotFound(new { error = "Client not found" });
            }

            // Get the product
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            // Get price from price list if client has a price list assigned
            decimal? priceListPrice = null;
            if (client.PriceListId.HasValue)
            {
                var priceListEntry = await _context.DomainPriceListEntries
                    .AsNoTracking()
                    .Where(p => p.PriceListId == client.PriceListId && p.ProductId == productId)
                    .FirstOrDefaultAsync();

                if (priceListEntry != null && priceListEntry.OutPrice.HasValue)
                {
                    priceListPrice = priceListEntry.OutPrice.Value;
                }
            }

            return Ok(new
            {
                clientId = client.Id,
                clientName = client.Title,
                priceListId = client.PriceListId,
                productId = product.Id,
                productName = product.ProductName,
                productPrice = product.BasePrice,
                priceListPrice
            });
        }
    }
}
