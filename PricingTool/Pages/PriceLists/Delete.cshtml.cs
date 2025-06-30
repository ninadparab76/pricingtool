using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages.PriceLists
{
    public class DeleteModel : PageModel
    {
        private readonly PriceToolContext _context;

        public DeleteModel(PriceToolContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PriceList PriceList { get; set; } = default!;

        public bool HasAssociatedClients { get; set; }
        public int EntriesCount { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var priceList = await _context.PriceLists
                .FirstOrDefaultAsync(m => m.Id == id);

            if (priceList == null)
            {
                return NotFound();
            }

            PriceList = priceList;

            // Check if any clients are using this price list
            HasAssociatedClients = await _context.Clients
                .AnyAsync(c => c.PriceListId == id);

            // Get count of entries for this price list
            EntriesCount = await _context.DomainPriceListEntries
                .CountAsync(e => e.PriceListId == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var priceList = await _context.PriceLists
                .FirstOrDefaultAsync(m => m.Id == PriceList.Id);

            if (priceList == null)
            {
                return NotFound();
            }

            // Check if any clients are using this price list
            bool hasAssociatedClients = await _context.Clients
                .AnyAsync(c => c.PriceListId == priceList.Id);

            if (hasAssociatedClients)
            {
                // Re-check and render the page with warning
                HasAssociatedClients = true;
                EntriesCount = await _context.DomainPriceListEntries
                    .CountAsync(e => e.PriceListId == priceList.Id);
                return Page();
            }

            // Delete all entries associated with this price list
            var entries = await _context.DomainPriceListEntries
                .Where(e => e.PriceListId == priceList.Id)
                .ToListAsync();

            _context.DomainPriceListEntries.RemoveRange(entries);

            // Delete the price list
            _context.PriceLists.Remove(priceList);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
