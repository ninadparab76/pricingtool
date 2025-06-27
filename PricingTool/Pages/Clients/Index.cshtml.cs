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
    public class IndexModel : PageModel
    {
        private readonly PriceToolContext _context;

        public IndexModel(PriceToolContext context)
        {
            _context = context;
        }

        public PaginatedList<Client> Client { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }
        
        public string ReferenceSort { get; set; }
        public string TitleSort { get; set; }
        public string PriceListSort { get; set; }
        public string DateCreatedSort { get; set; }
        public string DateModifiedSort { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentPriceListFilter { get; set; }
        public SelectList PriceListSelectList { get; set; }

        public async Task OnGetAsync(string sortOrder, string priceListFilter, int? pageIndex)
        {
            // Set up sorting
            CurrentSort = sortOrder;
            ReferenceSort = sortOrder == "reference_asc" ? "reference_desc" : "reference_asc";
            TitleSort = sortOrder == "title_asc" ? "title_desc" : "title_asc";
            PriceListSort = sortOrder == "pricelist_asc" ? "pricelist_desc" : "pricelist_asc";
            DateCreatedSort = sortOrder == "created_asc" ? "created_desc" : "created_asc";
            DateModifiedSort = sortOrder == "modified_asc" ? "modified_desc" : "modified_asc";
            
            CurrentPriceListFilter = priceListFilter;

            // Create base query
            IQueryable<Client> clientsQuery = _context.Clients
                .Include(c => c.PriceList)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrEmpty(SearchString))
            {
                clientsQuery = clientsQuery.Where(c => 
                    (c.ReferenceNumber != null && c.ReferenceNumber.Contains(SearchString)) ||
                    (c.Title != null && c.Title.Contains(SearchString)) ||
                    (c.PriceList != null && c.PriceList.Name.Contains(SearchString)));
            }

            if (!string.IsNullOrEmpty(priceListFilter) && Guid.TryParse(priceListFilter, out Guid priceListId))
            {
                clientsQuery = clientsQuery.Where(c => c.PriceListId == priceListId);
            }

            // Apply sorting
            clientsQuery = sortOrder switch
            {
                "reference_asc" => clientsQuery.OrderBy(c => c.ReferenceNumber),
                "reference_desc" => clientsQuery.OrderByDescending(c => c.ReferenceNumber),
                "title_asc" => clientsQuery.OrderBy(c => c.Title),
                "title_desc" => clientsQuery.OrderByDescending(c => c.Title),
                "pricelist_asc" => clientsQuery.OrderBy(c => c.PriceList.Name),
                "pricelist_desc" => clientsQuery.OrderByDescending(c => c.PriceList.Name),
                "created_asc" => clientsQuery.OrderBy(c => c.DateCreated),
                "created_desc" => clientsQuery.OrderByDescending(c => c.DateCreated),
                "modified_asc" => clientsQuery.OrderBy(c => c.DateModified),
                "modified_desc" => clientsQuery.OrderByDescending(c => c.DateModified),
                _ => clientsQuery.OrderBy(c => c.Title)
            };

            // Get dropdown data for price list filter
            PriceListSelectList = new SelectList(
                await _context.PriceLists
                    .OrderBy(p => p.Name)
                    .Select(p => new { p.Id, p.Name })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Name");

            const int pageSize = 10;
            Client = await PaginatedList<Client>.CreateAsync(
                clientsQuery, pageIndex ?? 1, pageSize);
        }
    }
}
