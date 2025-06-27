using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context.Models;

namespace PricingTool.Pages.ClientProductDiscounts
{
    public class DeleteModel : PageModel
    {
        private readonly PriceTool.Context.PriceToolContext _context;

        public DeleteModel(PriceTool.Context.PriceToolContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ClientProductDiscount? ClientProductDiscount { get; set; }
        
        // Price from the price list if available
        public decimal? PriceListPrice { get; set; }
        
        // Base price to use for calculations (either price list price or product base price)
        public decimal BasePrice { get; set; }
        
        // Calculate the final price
        public decimal FinalPrice { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ClientProductDiscount = await _context.ClientProductDiscounts
                .Include(c => c.Client)
                    .ThenInclude(c => c!.PriceList)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ClientProductDiscount == null)
            {
                return NotFound();
            }
            
            // Get price from the price list if client has a price list assigned
            if (ClientProductDiscount.Client?.PriceListId != null)
            {
                var priceListEntry = await _context.DomainPriceListEntries
                    .Where(p => p.PriceListId == ClientProductDiscount.Client.PriceListId &&
                               p.ProductId == ClientProductDiscount.ProductId)
                    .FirstOrDefaultAsync();
                
                if (priceListEntry != null && priceListEntry.OutPrice.HasValue)
                {
                    PriceListPrice = priceListEntry.OutPrice.Value;
                }
            }
            
            // Use price list price if available, otherwise use product base price
            BasePrice = PriceListPrice ?? ClientProductDiscount.Product!.BasePrice;
            
            // Calculate final price
            FinalPrice = ClientProductDiscount.FixedPrice ?? 
                (ClientProductDiscount.Discount.HasValue 
                    ? BasePrice * (1 - ClientProductDiscount.Discount.Value / 100) 
                    : BasePrice);
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ClientProductDiscount = await _context.ClientProductDiscounts.FindAsync(id);

            if (ClientProductDiscount != null)
            {
                _context.ClientProductDiscounts.Remove(ClientProductDiscount);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
