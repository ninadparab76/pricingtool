using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages_PriceListItems
{
    public class DetailsModel : PageModel
    {
        private readonly PriceTool.Context.PriceToolContext _context;

        public DetailsModel(PriceTool.Context.PriceToolContext context)
        {
            _context = context;
        }

        public PriceListEntry PriceListEntry { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pricelistentry = await _context.DomainPriceListEntries.FirstOrDefaultAsync(m => m.Id == id);
            if (pricelistentry == null)
            {
                return NotFound();
            }
            else
            {
                PriceListEntry = pricelistentry;
            }
            return Page();
        }
    }
}
