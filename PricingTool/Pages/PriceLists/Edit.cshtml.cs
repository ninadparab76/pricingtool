using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages.PriceLists
{
    public class EditModel : PageModel
    {
        private readonly PriceToolContext _context;

        public EditModel(PriceToolContext context)
        {
            _context = context;
            PriceListEntries = new List<PriceListEntry>();
        }

        [BindProperty]
        public PriceList PriceList { get; set; } = default!;

        public List<PriceListEntry> PriceListEntries { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var priceList = await _context.PriceLists.FirstOrDefaultAsync(m => m.Id == id);
            
            if (priceList == null)
            {
                return NotFound();
            }
            
            PriceList = priceList;

            // Load all entries for this price list with their products
            PriceListEntries = await _context.DomainPriceListEntries
                .Where(e => e.PriceListId == id)
                .Include(e => e.Product)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload entries in case of validation error
                PriceListEntries = await _context.DomainPriceListEntries
                    .Where(e => e.PriceListId == PriceList.Id)
                    .Include(e => e.Product)
                    .ToListAsync();
                    
                return Page();
            }

            var priceListToUpdate = await _context.PriceLists
                .FirstOrDefaultAsync(p => p.Id == PriceList.Id);

            if (priceListToUpdate == null)
            {
                return NotFound();
            }

            // Update only the fields that can be changed
            priceListToUpdate.Name = PriceList.Name;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PriceListExists(PriceList.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool PriceListExists(Guid id)
        {
            return _context.PriceLists.Any(e => e.Id == id);
        }
    }
}
