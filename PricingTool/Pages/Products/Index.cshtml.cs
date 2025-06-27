using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context.Models;
using System.Collections.Generic;

namespace PricingTool.Pages_Products
{
    public class IndexModel : PageModel
    {
        private readonly PriceTool.Context.PriceToolContext _context;

        public IndexModel(PriceTool.Context.PriceToolContext context)
        {
            _context = context;
        }

        public PaginatedList<Product> Product { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CurrentSort { get; set; }
        
        public string CategoryFilter { get; set; }
        public string TldFilter { get; set; }
        public SelectList CategorySelectList { get; set; }
        public SelectList TldSelectList { get; set; }

        // Grouped view properties
        [BindProperty(SupportsGet = true)]
        public bool GroupByEnabled { get; set; } = false;
        
        [BindProperty(SupportsGet = true)]
        public string GroupByField { get; set; } = "Category"; // Default to Category
        
        public List<ProductGroup> ProductGroups { get; set; } = new List<ProductGroup>();

        public string NameSort { get; set; }
        public string CategorySort { get; set; }
        public string TldSort { get; set; }
        public string PeriodSort { get; set; }
        public string PriceSort { get; set; }
        public string DateCreatedSort { get; set; }
        public string DateModifiedSort { get; set; }

        public async Task OnGetAsync(string sortOrder, string categoryFilter, string tldFilter, 
            bool? groupByEnabled, string groupByField, int? pageIndex, decimal? minPrice, decimal? maxPrice)
        {
            IQueryable<Product> productsIQ = _context.Products.AsNoTracking();
            
            // Set up sorting
            CurrentSort = sortOrder;
            NameSort = sortOrder == "name_asc" ? "name_desc" : "name_asc";
            CategorySort = sortOrder == "category_asc" ? "category_desc" : "category_asc";
            TldSort = sortOrder == "tld_asc" ? "tld_desc" : "tld_asc";
            PeriodSort = sortOrder == "period_asc" ? "period_desc" : "period_asc";
            PriceSort = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            DateCreatedSort = sortOrder == "created_asc" ? "created_desc" : "created_asc";
            DateModifiedSort = sortOrder == "modified_asc" ? "modified_desc" : "modified_asc";
            
            // Set up filters
            CategoryFilter = categoryFilter;
            TldFilter = tldFilter;
            
            // Set up grouping
            if (groupByEnabled.HasValue)
            {
                GroupByEnabled = groupByEnabled.Value;
            }
            
            if (!string.IsNullOrEmpty(groupByField))
            {
                GroupByField = groupByField;
            }

            // Apply filtering if search string is provided
            if (!string.IsNullOrEmpty(SearchString))
            {
                productsIQ = productsIQ.Where(p => 
                    p.ProductName.Contains(SearchString) || 
                    (p.Category != null && p.Category.Contains(SearchString)) || 
                    p.Tld.Contains(SearchString));
            }
            
            // Apply category filter
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                productsIQ = productsIQ.Where(p => p.Category == categoryFilter);
            }
            
            // Apply TLD filter
            if (!string.IsNullOrEmpty(tldFilter))
            {
                productsIQ = productsIQ.Where(p => p.Tld == tldFilter);
            }
            
            // Apply price filters
            if (minPrice.HasValue)
            {
                productsIQ = productsIQ.Where(p => p.BasePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsIQ = productsIQ.Where(p => p.BasePrice <= maxPrice.Value);
            }

            // Apply sorting
            productsIQ = sortOrder switch
            {
                "name_asc" => productsIQ.OrderBy(p => p.ProductName),
                "name_desc" => productsIQ.OrderByDescending(p => p.ProductName),
                "category_asc" => productsIQ.OrderBy(p => p.Category),
                "category_desc" => productsIQ.OrderByDescending(p => p.Category),
                "tld_asc" => productsIQ.OrderBy(p => p.Tld),
                "tld_desc" => productsIQ.OrderByDescending(p => p.Tld),
                "period_asc" => productsIQ.OrderBy(p => p.Period),
                "period_desc" => productsIQ.OrderByDescending(p => p.Period),
                "price_asc" => productsIQ.OrderBy(p => p.BasePrice),
                "price_desc" => productsIQ.OrderByDescending(p => p.BasePrice),
                "created_asc" => productsIQ.OrderBy(p => p.DateCreated),
                "created_desc" => productsIQ.OrderByDescending(p => p.DateCreated),
                "modified_asc" => productsIQ.OrderBy(p => p.DateModified),
                "modified_desc" => productsIQ.OrderByDescending(p => p.DateModified),
                _ => productsIQ.OrderBy(p => p.ProductName),
            };
            
            // Get distinct categories for the dropdown
            var categories = await _context.Products
                .Where(p => p.Category != null)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
                
            CategorySelectList = new SelectList(categories);
            
            // Get distinct TLDs for the dropdown
            var tlds = await _context.Products
                .Select(p => p.Tld)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
                
            TldSelectList = new SelectList(tlds);

            // If grouping is enabled, prepare the grouped view
            if (GroupByEnabled)
            {
                var allProducts = await productsIQ.ToListAsync();
                
                if (GroupByField == "Category")
                {
                    // Group by Category
                    var groupedProducts = allProducts
                        .GroupBy(p => p.Category ?? "Uncategorized")
                        .OrderBy(g => g.Key);
                        
                    foreach (var group in groupedProducts)
                    {
                        ProductGroups.Add(new ProductGroup
                        {
                            GroupName = group.Key,
                            Products = group.ToList()
                        });
                    }
                }
                else if (GroupByField == "TLD")
                {
                    // Group by TLD
                    var groupedProducts = allProducts
                        .GroupBy(p => p.Tld)
                        .OrderBy(g => g.Key);
                        
                    foreach (var group in groupedProducts)
                    {
                        ProductGroups.Add(new ProductGroup
                        {
                            GroupName = group.Key,
                            Products = group.ToList()
                        });
                    }
                }
                
                // For grouped view, we don't need pagination
                Product = null;
            }
            else
            {
                // For regular view, use pagination
                int pageSize = 15; // Increased page size to show more items per page
                Product = await PaginatedList<Product>.CreateAsync(productsIQ, pageIndex ?? 1, pageSize);
            }
        }
        
        // Class to represent a group of products for grouped view
        public class ProductGroup
        {
            public string GroupName { get; set; }
            public List<Product> Products { get; set; } = new List<Product>();
        }
    }
}
