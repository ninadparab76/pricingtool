using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages.Domains
{
    public class DeleteModel : PageModel
    {
        private readonly PriceToolContext _context;

        public DeleteModel(PriceToolContext context)
        {
            _context = context;
        }        [BindProperty]
        public Domain? Domain { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Domain = await _context.Domains
                .Include(d => d.Client)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (Domain == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Domain = await _context.Domains.FindAsync(id);

            if (Domain != null)
            {
                _context.Domains.Remove(Domain);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
