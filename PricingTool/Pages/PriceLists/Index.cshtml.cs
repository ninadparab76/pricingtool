using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PriceTool.Context;
using PriceTool.Context.Models;

namespace PricingTool.Pages.PriceLists
{
    public class IndexModel : PageModel
    {
        private readonly PriceToolContext _context;

        public IndexModel(PriceToolContext context)
        {
            _context = context;
            SearchString = string.Empty;
            NameSort = string.Empty;
            DateSort = string.Empty;
            CurrentSort = string.Empty;
            PriceListEntryCounts = new Dictionary<Guid, int>();
        }

        public List<PriceList> PriceLists { get; set; } = new();
        public Dictionary<Guid, int> PriceListEntryCounts { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public string NameSort { get; set; }
        public string DateSort { get; set; }
        public string CurrentSort { get; set; }
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public async Task OnGetAsync(string sortOrder, int? pageIndex)
        {
            // Set up sorting
            CurrentSort = sortOrder;
            NameSort = sortOrder == "name_asc" ? "name_desc" : "name_asc";
            DateSort = sortOrder == "date_asc" ? "date_desc" : "date_asc";

            // Set page index
            PageIndex = pageIndex ?? 1;
            int pageSize = 10;

            // Create base query
            IQueryable<PriceList> priceListsQuery = _context.PriceLists
                .AsNoTracking();

            // Apply search filter if specified
            if (!string.IsNullOrEmpty(SearchString))
            {
                priceListsQuery = priceListsQuery.Where(p => 
                    p.Name.Contains(SearchString));
            }

            // Apply sorting
            priceListsQuery = sortOrder switch
            {
                "name_asc" => priceListsQuery.OrderBy(p => p.Name),
                "name_desc" => priceListsQuery.OrderByDescending(p => p.Name),
                "date_asc" => priceListsQuery.OrderBy(p => p.DateCreated),
                "date_desc" => priceListsQuery.OrderByDescending(p => p.DateCreated),
                _ => priceListsQuery.OrderBy(p => p.Name)
            };

            // Calculate total pages
            var totalRecords = await priceListsQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // Get paginated data
            PriceLists = await priceListsQuery
                .Skip((PageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get count of entries for each price list
            var priceListIds = PriceLists.Select(p => p.Id).ToList();
            var entryCounts = await _context.DomainPriceListEntries
                .Where(e => priceListIds.Contains(e.PriceListId))
                .GroupBy(e => e.PriceListId)
                .Select(g => new { PriceListId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PriceListId, x => x.Count);

            PriceListEntryCounts = entryCounts;
        }
    }
}
