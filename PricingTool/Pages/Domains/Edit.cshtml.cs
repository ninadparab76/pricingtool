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
    public class EditModel : PageModel
    {
        private readonly PriceToolContext _context;

        public EditModel(PriceToolContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Domain Domain { get; set; }

        public SelectList ClientSelectList { get; set; }

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

            var domainToUpdate = await _context.Domains.FindAsync(Domain.Id);

            if (domainToUpdate == null)
            {
                return NotFound();
            }

            // Update timestamp
            domainToUpdate.DateModified = DateTime.UtcNow;

            // Update domain properties
            if (await TryUpdateModelAsync<Domain>(
                domainToUpdate,
                "Domain",
                d => d.DomainName, d => d.Tld, d => d.ClientId,
                d => d.ExpirationDate, d => d.Comment, d => d.HasRegistryLock,
                d => d.RenewPeriod, d => d.HasLocalPresence, d => d.HasPrivacy,
                d => d.HasProxy))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToPage("./Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DomainExists(Domain.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await PopulateSelectListsAsync();
            return Page();
        }

        private bool DomainExists(Guid id)
        {
            return _context.Domains.Any(e => e.Id == id);
        }
    }
}
