using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages.Domains
{
    public class CreateModel : PageModel
    {
        private readonly PriceToolContext _context;

        public CreateModel(PriceToolContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Domain Domain { get; set; }

        public SelectList ClientSelectList { get; set; }
        
        public async Task<IActionResult> OnGetAsync(Guid? clientId = null)
        {
            Domain = new Domain
            {
                ClientId = clientId ?? Guid.Empty,
                ExpirationDate = DateTime.Now.AddYears(1),
                HasPrivacy = true,
                RenewPeriod = 1
            };
            
            await PopulateSelectListsAsync();
            return Page();
        }

        private async Task PopulateSelectListsAsync()
        {
            // Get clients for dropdown
            ClientSelectList = new SelectList(
                await _context.Clients
                    .OrderBy(c => c.Title)
                    .Select(c => new { c.Id, c.Title })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Title");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync();
                return Page();
            }

            // Set timestamps
            Domain.DateCreated = DateTime.UtcNow;
            Domain.DateModified = DateTime.UtcNow;
            Domain.Id = Guid.NewGuid();

            _context.Domains.Add(Domain);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
