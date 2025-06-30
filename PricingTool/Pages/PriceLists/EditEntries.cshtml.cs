using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages.PriceLists
{
    public class EditEntriesModel : PageModel
    {
        private readonly PriceToolContext _context;

        public EditEntriesModel(PriceToolContext context)
        {
            _context = context;
            PriceListEntries = new List<PriceListEntry>();
            AvailableProducts = new List<Product>();
            Categories = new List<string>();
        }

        public PriceList PriceList { get; set; } = default!;
        public List<PriceListEntry> PriceListEntries { get; set; }
        public List<Product> AvailableProducts { get; set; }
        public List<string> Categories { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid priceListId)
        {
            var priceList = await _context.PriceLists.FirstOrDefaultAsync(m => m.Id == priceListId);
            
            if (priceList == null)
            {
                return NotFound();
            }
            
            PriceList = priceList;

            // Load existing entries
            await LoadExistingEntries(priceListId);

            // Get products that are not in the price list
            var existingProductIds = PriceListEntries.Select(e => e.ProductId).ToList();
            AvailableProducts = await _context.Products
                .Where(p => !existingProductIds.Contains(p.Id))
                .OrderBy(p => p.ProductName)
                .ThenBy(p => p.Tld)
                .ToListAsync();

            // Get unique categories
            Categories = await _context.Products
                .Select(p => p.Category)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .OrderBy(c => c)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddProductsAsync(Guid priceListId, List<Guid> selectedProductIds)
        {
            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                return RedirectToPage("./EditEntries", new { priceListId });
            }

            var entries = selectedProductIds.Select(productId => new PriceListEntry
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                PriceListId = priceListId,
                // Default OutPrice to null or can set to base price
                OutPrice = null
            }).ToList();

            _context.DomainPriceListEntries.AddRange(entries);
            await _context.SaveChangesAsync();

            return RedirectToPage("./EditEntries", new { priceListId });
        }

        public async Task<IActionResult> OnPostUpdatePriceAsync(Guid entryId, Guid priceListId, decimal? outPrice)
        {
            var entry = await _context.DomainPriceListEntries.FindAsync(entryId);
            
            if (entry == null)
            {
                return NotFound();
            }

            entry.OutPrice = outPrice;
            await _context.SaveChangesAsync();

            return RedirectToPage("./EditEntries", new { priceListId });
        }

        public async Task<IActionResult> OnPostRemoveEntryAsync(Guid entryId, Guid priceListId)
        {
            var entry = await _context.DomainPriceListEntries.FindAsync(entryId);
            
            if (entry == null)
            {
                return NotFound();
            }

            _context.DomainPriceListEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return RedirectToPage("./EditEntries", new { priceListId });
        }

        private async Task LoadExistingEntries(Guid priceListId)
        {            PriceListEntries = await _context.DomainPriceListEntries
                .Where(e => e.PriceListId == priceListId)
                .Include(e => e.Product)
                .OrderBy(e => e.Product != null ? e.Product.ProductName : string.Empty)
                .ThenBy(e => e.Product != null ? e.Product.Tld : string.Empty)
                .ToListAsync();
        }
    }
}
