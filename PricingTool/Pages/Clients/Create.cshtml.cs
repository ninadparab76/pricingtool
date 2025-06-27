using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages_Clients
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
            ViewData["PriceListId"] = new SelectList(_context.Set<PriceList>(), "Id", "Name");
            return Page();
        }

        [BindProperty]
        public Client Client { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Clients.Add(Client);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
