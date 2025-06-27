using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context.Models;
using System.Linq.Dynamic.Core;

namespace PricingTool.Pages.ClientProductDiscounts
{
    public class IndexModel : PageModel
    {
        private readonly PriceTool.Context.PriceToolContext _context;

        public IndexModel(PriceTool.Context.PriceToolContext context)
        {
            _context = context;
        }

        // List of all ClientProductDiscounts with pagination
        public PaginatedList<ClientProductDiscount>? ClientProductDiscounts { get; set; }
        
        // List of clients with their discounts (grouped view)
        public List<ClientWithDiscounts> ClientsWithDiscounts { get; set; } = new List<ClientWithDiscounts>();
        
        // Dictionary to store price list prices for products
        public Dictionary<Guid, Dictionary<Guid, decimal>> PriceListPrices { get; set; } = new Dictionary<Guid, Dictionary<Guid, decimal>>();
        
        // Search and filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? ClientFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? ProductFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? CurrentSort { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public bool GroupByClient { get; set; } = true;
        
        // Filter options for dropdowns
        public SelectList? ClientSelectList { get; set; }
        public SelectList? ProductSelectList { get; set; }
        
        // Sort option properties
        public string? ClientSort { get; set; }
        public string? ProductSort { get; set; }
        public string? FixedPriceSort { get; set; }
        public string? DiscountSort { get; set; }
        public string? DateCreatedSort { get; set; }
        public string? DateModifiedSort { get; set; }
        
        public async Task OnGetAsync(string? sortOrder, 
            string? clientFilter, 
            string? productFilter, 
            bool? groupByClient,
            decimal? minFixedPrice, 
            decimal? maxFixedPrice, 
            decimal? minDiscount, 
            decimal? maxDiscount, 
            int? pageIndex)
        {
            // Set default grouping option
            if (groupByClient.HasValue)
            {
                GroupByClient = groupByClient.Value;
            }
            
            // Set up sorting
            CurrentSort = sortOrder;
            ClientSort = sortOrder == "client_asc" ? "client_desc" : "client_asc";
            ProductSort = sortOrder == "product_asc" ? "product_desc" : "product_asc";
            FixedPriceSort = sortOrder == "fixedprice_asc" ? "fixedprice_desc" : "fixedprice_asc";
            DiscountSort = sortOrder == "discount_asc" ? "discount_desc" : "discount_asc";
            DateCreatedSort = sortOrder == "created_asc" ? "created_desc" : "created_asc";
            DateModifiedSort = sortOrder == "modified_asc" ? "modified_desc" : "modified_asc";
            
            // Save filter values
            ClientFilter = clientFilter;
            ProductFilter = productFilter;
            
            // Start with all discounts and include related entities
            IQueryable<ClientProductDiscount> discountsQuery = _context.ClientProductDiscounts
                .Include(c => c.Client)
                    .ThenInclude(c => c!.PriceList)
                .Include(c => c.Product)
                .AsNoTracking();
              // Apply search filter if provided
            if (!string.IsNullOrEmpty(SearchString))
            {
                discountsQuery = discountsQuery.Where(d => 
                    (d.Client != null && (d.Client.Title != null && d.Client.Title.Contains(SearchString) || 
                                         (d.Client.ReferenceNumber != null && d.Client.ReferenceNumber.Contains(SearchString)))) ||
                    (d.Product != null && d.Product.ProductName != null && d.Product.ProductName.Contains(SearchString)));
            }
            
            // Apply client filter if provided
            if (!string.IsNullOrEmpty(ClientFilter))
            {
                var clientId = Guid.Parse(ClientFilter);
                discountsQuery = discountsQuery.Where(d => d.ClientId == clientId);
            }
            
            // Apply product filter if provided
            if (!string.IsNullOrEmpty(ProductFilter))
            {
                var productId = Guid.Parse(ProductFilter);
                discountsQuery = discountsQuery.Where(d => d.ProductId == productId);
            }
            
            // Apply price range filters if provided
            if (minFixedPrice.HasValue)
            {
                discountsQuery = discountsQuery.Where(d => d.FixedPrice >= minFixedPrice.Value);
            }
            
            if (maxFixedPrice.HasValue)
            {
                discountsQuery = discountsQuery.Where(d => d.FixedPrice <= maxFixedPrice.Value);
            }
            
            // Apply discount range filters if provided
            if (minDiscount.HasValue)
            {
                discountsQuery = discountsQuery.Where(d => d.Discount >= minDiscount.Value);
            }
            
            if (maxDiscount.HasValue)
            {
                discountsQuery = discountsQuery.Where(d => d.Discount <= maxDiscount.Value);
            }
            
            // Apply sorting
            discountsQuery = sortOrder switch
            {
                "client_asc" => discountsQuery.OrderBy(d => d.Client!.Title),
                "client_desc" => discountsQuery.OrderByDescending(d => d.Client!.Title),
                "product_asc" => discountsQuery.OrderBy(d => d.Product!.ProductName),
                "product_desc" => discountsQuery.OrderByDescending(d => d.Product!.ProductName),
                "fixedprice_asc" => discountsQuery.OrderBy(d => d.FixedPrice),
                "fixedprice_desc" => discountsQuery.OrderByDescending(d => d.FixedPrice),
                "discount_asc" => discountsQuery.OrderBy(d => d.Discount),
                "discount_desc" => discountsQuery.OrderByDescending(d => d.Discount),
                "created_asc" => discountsQuery.OrderBy(d => d.DateCreated),
                "created_desc" => discountsQuery.OrderByDescending(d => d.DateCreated),
                "modified_asc" => discountsQuery.OrderBy(d => d.DateModified),
                "modified_desc" => discountsQuery.OrderByDescending(d => d.DateModified),
                _ => discountsQuery.OrderBy(d => d.Client!.Title).ThenBy(d => d.Product!.ProductName),
            };
            
            // Load all price list entries to cache price list prices
            await LoadPriceListPricesAsync();
            
            // Get filter options for dropdowns
            await LoadFilterOptionsAsync();
            
            // If grouping by client is enabled, prepare the grouped view
            if (GroupByClient)
            {
                // Get all clients that have discounts
                var clientsWithDiscountsQuery = await _context.Clients
                    .Where(c => _context.ClientProductDiscounts.Any(d => d.ClientId == c.Id))
                    .OrderBy(c => c.Title)
                    .ToListAsync();
                
                foreach (var client in clientsWithDiscountsQuery)
                {
                    // For each client, get their discounts with filtering applied
                    var clientDiscounts = await discountsQuery
                        .Where(d => d.ClientId == client.Id)
                        .ToListAsync();
                    
                    // Only add clients that have discounts after filtering
                    if (clientDiscounts.Any())
                    {
                        ClientsWithDiscounts.Add(new ClientWithDiscounts
                        {
                            Client = client,
                            Discounts = clientDiscounts
                        });
                    }
                }
                
                // For grouped view, we'll handle pagination on the UI
                ClientProductDiscounts = null;
            }
            else
            {
                // For regular view, use pagination
                int pageSize = 10;
                ClientProductDiscounts = await PaginatedList<ClientProductDiscount>.CreateAsync(
                    discountsQuery, pageIndex ?? 1, pageSize);
            }
        }
        
        private async Task LoadPriceListPricesAsync()
        {
            // Get all price list entries to cache price list prices
            var priceListEntries = await _context.DomainPriceListEntries
                .AsNoTracking()
                .ToListAsync();
                
            foreach (var entry in priceListEntries)
            {
                if (entry.OutPrice.HasValue)
                {
                    if (!PriceListPrices.ContainsKey(entry.PriceListId))
                    {
                        PriceListPrices[entry.PriceListId] = new Dictionary<Guid, decimal>();
                    }
                    
                    PriceListPrices[entry.PriceListId][entry.ProductId] = entry.OutPrice.Value;
                }
            }
        }
        
        // Helper method to get price from price list
        public decimal GetBasePrice(ClientProductDiscount discount)
        {
            if (discount.Client?.PriceListId != null && 
                PriceListPrices.ContainsKey(discount.Client.PriceListId.Value) && 
                PriceListPrices[discount.Client.PriceListId.Value].ContainsKey(discount.ProductId))
            {
                return PriceListPrices[discount.Client.PriceListId.Value][discount.ProductId];
            }
            
            return discount.Product?.BasePrice ?? 0;
        }
        
        // Helper method to calculate final price
        public decimal GetFinalPrice(ClientProductDiscount discount)
        {
            var basePrice = GetBasePrice(discount);
            
            return discount.FixedPrice.HasValue ? 
                basePrice - discount.FixedPrice.Value : 
                (discount.Discount.HasValue 
                    ? basePrice * (1 - discount.Discount.Value / 100) 
                    : basePrice);
        }
        
        private async Task LoadFilterOptionsAsync()
        {
            // Load clients for dropdown
            var clients = await _context.Clients
                .OrderBy(c => c.Title)
                .Select(c => new { c.Id, DisplayName = $"{c.Title} {(string.IsNullOrEmpty(c.ReferenceNumber) ? "" : $"({c.ReferenceNumber})")}" })
                .ToListAsync();
                
            ClientSelectList = new SelectList(clients, "Id", "DisplayName");
            
            // Load products for dropdown
            var products = await _context.Products
                .OrderBy(p => p.ProductName)
                .Select(p => new { p.Id, p.ProductName })
                .ToListAsync();
                
            ProductSelectList = new SelectList(products, "Id", "ProductName");
        }
        
        // Class to represent a client with their discounts for grouped view
        public class ClientWithDiscounts
        {
            public Client? Client { get; set; }
            public List<ClientProductDiscount> Discounts { get; set; } = new List<ClientProductDiscount>();
        }
    }
}
