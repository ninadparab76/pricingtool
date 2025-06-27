using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context.Models;

namespace PricingTool.Pages.ClientProductDiscounts
{
    public class EditModel : PageModel
    {
        private readonly PriceTool.Context.PriceToolContext _context;

        public EditModel(PriceTool.Context.PriceToolContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ClientProductDiscount? ClientProductDiscount { get; set; }
        
        public SelectList? ClientSelectList { get; set; }
        public SelectList? ProductSelectList { get; set; }
        
        public Client? Client { get; set; }
        public Product? Product { get; set; }
        
        // Price from the price list if available
        public decimal? PriceListPrice { get; set; }
        
        // Base price to use for calculations (either price list price or product base price)
        public decimal BasePrice { get; set; }
        
        // Final price after discount
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
            
            // Save references for display purposes
            Client = ClientProductDiscount.Client;
            Product = ClientProductDiscount.Product;
            
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
            
            await LoadSelectListsAsync();
            
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync();
                return Page();
            }

            // Validate that at least one of FixedPrice or Discount is provided
            if (ClientProductDiscount != null && !ClientProductDiscount.FixedPrice.HasValue && !ClientProductDiscount.Discount.HasValue)
            {
                ModelState.AddModelError(string.Empty, "You must provide either a Fixed Price or a Discount percentage.");
                await LoadSelectListsAsync();
                return Page();
            }

            if (ClientProductDiscount != null)
            {
                // Check if a discount for the same client and product already exists (excluding current record)
                var existingDiscount = await _context.ClientProductDiscounts
                    .Where(d => d.Id != ClientProductDiscount.Id)
                    .FirstOrDefaultAsync(d => 
                        d.ClientId == ClientProductDiscount.ClientId && 
                        d.ProductId == ClientProductDiscount.ProductId);
                        
                if (existingDiscount != null)
                {
                    ModelState.AddModelError(string.Empty, "A discount for this client and product combination already exists.");
                    await LoadSelectListsAsync();
                    return Page();
                }

                // Get current entity to preserve created date
                var existingEntity = await _context.ClientProductDiscounts.FindAsync(ClientProductDiscount.Id);
                if (existingEntity == null)
                {
                    return NotFound();
                }

                // Update properties
                existingEntity.ClientId = ClientProductDiscount.ClientId;
                existingEntity.ProductId = ClientProductDiscount.ProductId;
                existingEntity.FixedPrice = ClientProductDiscount.FixedPrice;
                existingEntity.Discount = ClientProductDiscount.Discount;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientProductDiscountExists(ClientProductDiscount.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ClientProductDiscountExists(Guid id)
        {
            return _context.ClientProductDiscounts.Any(e => e.Id == id);
        }

        private async Task LoadSelectListsAsync()
        {
            // Load clients for dropdown
            var clients = await _context.Clients
                .OrderBy(c => c.Title)
                .Select(c => new { c.Id, DisplayName = $"{c.Title} {(string.IsNullOrEmpty(c.ReferenceNumber) ? "" : $"({c.ReferenceNumber})")}" })
                .ToListAsync();
                
            ClientSelectList = new SelectList(clients, "Id", "DisplayName", ClientProductDiscount?.ClientId);
            
            // Load products for dropdown
            var products = await _context.Products
                .OrderBy(p => p.ProductName)
                .Select(p => new { 
                    p.Id, 
                    DisplayName = $"{p.ProductName} - {p.Category} - {p.BasePrice:C2}"
                })
                .ToListAsync();
                
            ProductSelectList = new SelectList(products, "Id", "DisplayName", ClientProductDiscount?.ProductId);
        }
    }
}
