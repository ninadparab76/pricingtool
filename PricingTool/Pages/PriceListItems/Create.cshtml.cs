using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context.Models;

namespace PricingTool.Pages_PriceListItems
{
    public class CreateModel : PageModel
    {
        private readonly PriceTool.Context.PriceToolContext _context;

        public CreateModel(PriceTool.Context.PriceToolContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGet()
        {            
            // Load only the necessary fields (Id and Name) to reduce data transfer
            // Add Take(100) to limit the number of items if there are too many
            ViewData["PriceListId"] = new SelectList(
                await _context.PriceLists
                    .OrderBy(p => p.Name)
                    .Select(p => new { p.Id, p.Name })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Name");

            // For products, limit to first 100 items ordered by name and only select needed fields
            ViewData["ProductId"] = new SelectList(
                await _context.Products
                    .OrderBy(x => x.ProductName)
                    .Select(p => new { p.Id, p.ProductName })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "ProductName");

            return Page();
        }

        [BindProperty]
        public PriceListEntry PriceListEntry { get; set; } = default!;        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload the dropdown data if validation fails
                // Load only the necessary fields (Id and Name) to reduce data transfer
                ViewData["PriceListId"] = new SelectList(
                    await _context.PriceLists.OrderBy(p => p.Name)
                    .Select(p => new { p.Id, p.Name })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Name", PriceListEntry.PriceListId);

                ViewData["ProductId"] = new SelectList(
                    await _context.Products
                        .OrderBy(x => x.ProductName)
                        .Select(p => new { p.Id, p.ProductName })
                        .Take(100)
                        .AsNoTracking()
                        .ToListAsync(),
                    "Id", "ProductName", PriceListEntry.ProductId);

                return Page();
            }

            // No need to set the dates manually as they will be set by the DbContext
            // Use Add rather than Attach to improve performance
            _context.DomainPriceListEntries.Add(PriceListEntry);

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException)
            {
                // Log the error (uncomment ex variable name and write a log)
                ModelState.AddModelError("", "Unable to save changes. " +
                    "Try again, and if the problem persists, " +
                    "see your system administrator.");
                return Page();
            }
        }
    }
}
