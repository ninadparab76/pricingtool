using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages.PriceLists
{
    public class CreateModel : PageModel
    {
        private readonly PriceToolContext _context;

        public CreateModel(PriceToolContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public PriceList PriceList { get; set; } = new();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Set the ID for the new price list
            PriceList.Id = Guid.NewGuid();

            _context.PriceLists.Add(PriceList);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
