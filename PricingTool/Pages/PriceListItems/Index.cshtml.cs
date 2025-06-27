using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context.Models;

namespace PricingTool.Pages_PriceListItems
{
    public class IndexModel : PageModel
    {
        private readonly PriceTool.Context.PriceToolContext _context;

        public IndexModel(PriceTool.Context.PriceToolContext context)
        {
            _context = context;
        }

        // Sorting properties
        public string ProductSort { get; set; }
        public string PriceListSort { get; set; }
        public string PriceSort { get; set; }
        public string DateCreatedSort { get; set; }
        public string DateModifiedSort { get; set; }
        
        // Filter properties
        public string CurrentFilter { get; set; }
        public string CurrentProductFilter { get; set; }
        public string CurrentPriceListFilter { get; set; }
        public string CurrentSort { get; set; }
        
        // Dropdown lists
        public SelectList PriceListSelectList { get; set; }
        public SelectList ProductSelectList { get; set; }
        
        // Pagination for regular view
        public PaginatedList<PriceListEntry> PriceListEntry { get; set; }
        
        // Group by Price List option
        [BindProperty(SupportsGet = true)]
        public bool GroupByPriceList { get; set; } = false;
        
        // List for grouped view
        public List<PriceListWithEntries> PriceListsWithEntries { get; set; } = new List<PriceListWithEntries>();

        public async Task OnGetAsync(string sortOrder, string searchString, string productFilter,
            string priceListFilter, bool? groupByPriceList, int? pageIndex, decimal? minPrice, decimal? maxPrice)
        {
            // Set grouping option
            if (groupByPriceList.HasValue)
            {
                GroupByPriceList = groupByPriceList.Value;
            }
            
            // Save sort/filter state for paging links
            CurrentSort = sortOrder;
            ProductSort = sortOrder == "product_asc" ? "product_desc" : "product_asc";
            PriceListSort = sortOrder == "pricelist_asc" ? "pricelist_desc" : "pricelist_asc";
            PriceSort = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            DateCreatedSort = sortOrder == "created_asc" ? "created_desc" : "created_asc";
            DateModifiedSort = sortOrder == "modified_asc" ? "modified_desc" : "modified_asc";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = CurrentFilter;
            }

            CurrentFilter = searchString;
            CurrentProductFilter = productFilter;
            CurrentPriceListFilter = priceListFilter;

            // Create base query
            IQueryable<PriceListEntry> priceListEntriesQuery = _context.DomainPriceListEntries
                .Include(p => p.PriceList)
                .Include(p => p.Product)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                priceListEntriesQuery = priceListEntriesQuery.Where(p =>
                    p.Product.ProductName.Contains(searchString) ||
                    p.PriceList.Name.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(productFilter) && Guid.TryParse(productFilter, out Guid productId))
            {
                priceListEntriesQuery = priceListEntriesQuery.Where(p => p.ProductId == productId);
            }

            if (!string.IsNullOrEmpty(priceListFilter) && Guid.TryParse(priceListFilter, out Guid priceListId))
            {
                priceListEntriesQuery = priceListEntriesQuery.Where(p => p.PriceListId == priceListId);
            }

            if (minPrice.HasValue)
            {
                priceListEntriesQuery = priceListEntriesQuery.Where(p => p.OutPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                priceListEntriesQuery = priceListEntriesQuery.Where(p => p.OutPrice <= maxPrice.Value);
            }

            // Apply sorting
            priceListEntriesQuery = sortOrder switch
            {
                "product_asc" => priceListEntriesQuery.OrderBy(p => p.Product.ProductName),
                "product_desc" => priceListEntriesQuery.OrderByDescending(p => p.Product.ProductName),
                "pricelist_asc" => priceListEntriesQuery.OrderBy(p => p.PriceList.Name),
                "pricelist_desc" => priceListEntriesQuery.OrderByDescending(p => p.PriceList.Name),
                "price_asc" => priceListEntriesQuery.OrderBy(p => p.OutPrice),
                "price_desc" => priceListEntriesQuery.OrderByDescending(p => p.OutPrice),
                "created_asc" => priceListEntriesQuery.OrderBy(p => p.DateCreated),
                "created_desc" => priceListEntriesQuery.OrderByDescending(p => p.DateCreated),
                "modified_asc" => priceListEntriesQuery.OrderBy(p => p.DateModified),
                "modified_desc" => priceListEntriesQuery.OrderByDescending(p => p.DateModified),
                _ => priceListEntriesQuery.OrderByDescending(p => p.DateModified)
            };

            // Get dropdown data for filters (using projection for efficiency)
            ProductSelectList = new SelectList(
                await _context.Products
                    .OrderBy(p => p.ProductName)
                    .Select(p => new { p.Id, p.ProductName })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "ProductName");

            PriceListSelectList = new SelectList(
                await _context.PriceLists
                    .OrderBy(p => p.Name)
                    .Select(p => new { p.Id, p.Name })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Name");

            // If grouping by price list is enabled, prepare the grouped view
            if (GroupByPriceList)
            {
                // Get all price lists that have entries
                var priceListsWithEntriesQuery = await _context.PriceLists
                    .Where(p => _context.DomainPriceListEntries.Any(e => e.PriceListId == p.Id))
                    .OrderBy(p => p.Name)
                    .ToListAsync();
                
                foreach (var priceList in priceListsWithEntriesQuery)
                {
                    // For each price list, get its entries with filtering applied
                    var priceListEntries = await priceListEntriesQuery
                        .Where(e => e.PriceListId == priceList.Id)
                        .ToListAsync();
                    
                    // Only add price lists that have entries after filtering
                    if (priceListEntries.Any())
                    {
                        PriceListsWithEntries.Add(new PriceListWithEntries
                        {
                            PriceList = priceList,
                            Entries = priceListEntries
                        });
                    }
                }
                
                // For grouped view, we don't need pagination
                PriceListEntry = null;
            }
            else
            {
                // For regular view, use pagination
                const int pageSize = 10;
                PriceListEntry = await PaginatedList<PriceListEntry>.CreateAsync(
                    priceListEntriesQuery, pageIndex ?? 1, pageSize);
            }
        }
        
        // Class to represent a price list with its entries for grouped view
        public class PriceListWithEntries
        {
            public PriceList PriceList { get; set; }
            public List<PriceListEntry> Entries { get; set; } = new List<PriceListEntry>();
        }
    }
}
