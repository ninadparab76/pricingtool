using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context.Models;

namespace PricingTool.Pages_PriceListItems
{
    public class EditModel : PageModel
    {
        private readonly PriceTool.Context.PriceToolContext _context;

        public EditModel(PriceTool.Context.PriceToolContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PriceListEntry PriceListEntry { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Improved query - only fetch what's needed and use AsNoTracking for read-only operation
            var pricelistentry = await _context.DomainPriceListEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pricelistentry == null)
            {
                return NotFound();
            }
            PriceListEntry = pricelistentry;

            // Load only necessary fields for dropdowns and use proper ordering
            ViewData["PriceListId"] = new SelectList(
                await _context.PriceLists
                    .OrderBy(p => p.Name)
                    .Select(p => new { p.Id, p.Name })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Name", PriceListEntry.PriceListId);

            ViewData["ProductId"] = new SelectList(
                await _context.Products
                    .OrderBy(p => p.ProductName)
                    .Select(p => new { p.Id, p.ProductName })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "ProductName", PriceListEntry.ProductId);

            return Page();
        }        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload the dropdown data if validation fails
                ViewData["PriceListId"] = new SelectList(
                    await _context.PriceLists
                        .OrderBy(p => p.Name)
                        .Select(p => new { p.Id, p.Name })
                        .AsNoTracking()
                        .ToListAsync(),
                    "Id", "Name", PriceListEntry.PriceListId);

                ViewData["ProductId"] = new SelectList(
                    await _context.Products
                        .OrderBy(p => p.ProductName)
                        .Select(p => new { p.Id, p.ProductName })
                        .AsNoTracking()
                        .ToListAsync(),
                    "Id", "ProductName", PriceListEntry.ProductId);

                return Page();
            }

            // DateModified will be updated automatically by the DbContext
            try
            {
                // More efficient update approach
                _context.Update(PriceListEntry);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PriceListEntryExists(PriceListEntry.Id))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError("", "The record was modified by another user. Please try again.");
                    return Page();
                }
            }

            return RedirectToPage("./Index");
        }

        private bool PriceListEntryExists(Guid id)
        {
            return _context.DomainPriceListEntries.Any(e => e.Id == id);
        }
    }
}
