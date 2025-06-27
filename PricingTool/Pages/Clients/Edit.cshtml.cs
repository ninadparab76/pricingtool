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

namespace PricingTool.Pages_Clients
{
    public class EditModel : PageModel
    {
        private readonly PriceToolContext _context;

        public EditModel(PriceToolContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Client Client { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client =  await _context.Clients.FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return NotFound();
            }
            Client = client;
           ViewData["PriceListId"] = new SelectList(_context.Set<PriceList>(), "Id", "Name");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Client).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(Client.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ClientExists(Guid id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }
    }
}
