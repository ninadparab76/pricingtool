using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context.Models;

namespace PricingTool.Pages.ClientProductDiscounts
{
    public class CreateModel : PageModel
    {
        private readonly PriceTool.Context.PriceToolContext _context;

        public CreateModel(PriceTool.Context.PriceToolContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectListsAsync();
            return Page();
        }

        [BindProperty]
        public ClientProductDiscount? ClientProductDiscount { get; set; }

        public SelectList? ClientSelectList { get; set; }
        public SelectList? ProductSelectList { get; set; }

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
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
                // Check if a discount for the same client and product already exists
                var existingDiscount = await _context.ClientProductDiscounts
                    .FirstOrDefaultAsync(d => 
                        d.ClientId == ClientProductDiscount.ClientId && 
                        d.ProductId == ClientProductDiscount.ProductId);
                        
                if (existingDiscount != null)
                {
                    ModelState.AddModelError(string.Empty, "A discount for this client and product combination already exists.");
                    await LoadSelectListsAsync();
                    return Page();
                }

                _context.ClientProductDiscounts.Add(ClientProductDiscount);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadSelectListsAsync()
        {
            // Load clients for dropdown
            var clients = await _context.Clients
                .OrderBy(c => c.Title)
                .Select(c => new { c.Id, DisplayName = $"{c.Title} {(string.IsNullOrEmpty(c.ReferenceNumber) ? "" : $"({c.ReferenceNumber})")}" })
                .ToListAsync();
                
            ClientSelectList = new SelectList(clients, "Id", "DisplayName");
            
            // Get all price list entries to use in product descriptions
            var priceListEntries = await _context.DomainPriceListEntries
                .AsNoTracking()
                .ToListAsync();

            // Create a dictionary to quickly look up price list entries
            var priceListPrices = new Dictionary<Guid, Dictionary<Guid, decimal>>();
            foreach (var entry in priceListEntries)
            {
                if (entry.OutPrice.HasValue)
                {
                    if (!priceListPrices.ContainsKey(entry.PriceListId))
                    {
                        priceListPrices[entry.PriceListId] = new Dictionary<Guid, decimal>();
                    }
                    
                    priceListPrices[entry.PriceListId][entry.ProductId] = entry.OutPrice.Value;
                }
            }
            
            // Load products for dropdown including price list price info if available
            var productsWithPrices = await _context.Products
                .OrderBy(p => p.ProductName)
                .Select(p => new { 
                    p.Id, 
                    p.ProductName,
                    p.Category,
                    p.BasePrice,
                    DisplayInfo = $"{p.ProductName} - {p.Category} - {p.BasePrice:C2}"
                })
                .ToListAsync();
                
            // Enhanced products with price list info
            var enhancedProducts = productsWithPrices.Select(p => new
            {
                p.Id,
                DisplayName = p.DisplayInfo
            }).ToList();
                
            ProductSelectList = new SelectList(enhancedProducts, "Id", "DisplayName");
        }
    }
}
