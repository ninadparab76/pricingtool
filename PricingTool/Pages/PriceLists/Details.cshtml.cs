using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages.PriceLists
{
    public class DetailsModel : PageModel
    {
        private readonly PriceToolContext _context;

        public DetailsModel(PriceToolContext context)
        {
            _context = context;
            PriceListEntries = new List<PriceListEntry>();
            Clients = new List<Client>();
        }

        public PriceList PriceList { get; set; } = default!;
        public List<PriceListEntry> PriceListEntries { get; set; }
        public List<Client> Clients { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var priceList = await _context.PriceLists.FirstOrDefaultAsync(m => m.Id == id);
            
            if (priceList == null)
            {
                return NotFound();
            }
            
            PriceList = priceList;            // Load all entries for this price list with their products
            PriceListEntries = await _context.DomainPriceListEntries
                .Where(e => e.PriceListId == id)
                .Include(e => e.Product)
                .OrderBy(e => e.Product != null ? e.Product.ProductName : string.Empty)
                .ToListAsync();

            // Get clients using this price list
            Clients = await _context.Clients
                .Where(c => c.PriceListId == id)
                .ToListAsync();

            return Page();
        }
    }
}
