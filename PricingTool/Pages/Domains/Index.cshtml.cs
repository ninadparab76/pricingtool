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

namespace PricingTool.Pages.Domains
{
    public class IndexModel : PageModel
    {        private readonly PriceToolContext _context;

        public IndexModel(PriceToolContext context)
        {
            _context = context;
            DomainRenewalCostBreakdown = new Dictionary<Guid, Dictionary<string, decimal>>();
            
            // Initialize required properties
            SearchString = string.Empty;
            DomainNameSort = string.Empty;
            TldSort = string.Empty;
            ClientSort = string.Empty;
            ExpirationDateSort = string.Empty;
            DateCreatedSort = string.Empty;
            CurrentSort = string.Empty;
            CurrentClientFilter = string.Empty;
            ClientSelectList = new SelectList(Enumerable.Empty<Client>(), "Id", "Title");
            Domains = new PaginatedList<Domain>(new List<Domain>(), 0, 1, 10);
        }        public required PaginatedList<Domain> Domains { get; set; }
        public Dictionary<Guid, List<Domain>> GroupedDomains { get; set; } = new();
        public Dictionary<Guid, string> ClientNames { get; set; } = new();
        public Dictionary<Guid, decimal> ClientTotalRenewalCosts { get; set; } = new();
        public Dictionary<Guid, decimal> DomainRenewalCosts { get; set; } = new();
        public Dictionary<Guid, Dictionary<string, decimal>> DomainRenewalCostBreakdown { get; set; } = new();
        
        [BindProperty(SupportsGet = true)]
        public required string SearchString { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public bool GroupByClient { get; set; }
        
        public required string DomainNameSort { get; set; }
        public required string TldSort { get; set; }
        public required string ClientSort { get; set; }
        public required string ExpirationDateSort { get; set; }
        public required string DateCreatedSort { get; set; }
        public required string CurrentSort { get; set; }
        public required string CurrentClientFilter { get; set; }
        public required SelectList ClientSelectList { get; set; }

        public async Task OnGetAsync(string sortOrder, string clientFilter, int? pageIndex, bool groupByClient = false)
        {
            // Set up sorting
            CurrentSort = sortOrder;
            DomainNameSort = sortOrder == "domain_asc" ? "domain_desc" : "domain_asc";
            TldSort = sortOrder == "tld_asc" ? "tld_desc" : "tld_asc";
            ClientSort = sortOrder == "client_asc" ? "client_desc" : "client_asc";
            ExpirationDateSort = sortOrder == "expiration_asc" ? "expiration_desc" : "expiration_asc";
            DateCreatedSort = sortOrder == "created_asc" ? "created_desc" : "created_asc";
            
            CurrentClientFilter = clientFilter;
            GroupByClient = groupByClient;

            // Create base query
            IQueryable<Domain> domainsQuery = _context.Domains
                .Include(d => d.Client)
                .AsNoTracking();

            // Apply filters
            if (!string.IsNullOrEmpty(SearchString))
            {
                domainsQuery = domainsQuery.Where(d => 
                    (d.DomainName != null && d.DomainName.Contains(SearchString)) ||
                    (d.Tld != null && d.Tld.Contains(SearchString)) ||
                    (d.Client != null && d.Client.Title != null && d.Client.Title.Contains(SearchString)));
            }

            if (!string.IsNullOrEmpty(clientFilter) && Guid.TryParse(clientFilter, out Guid clientId))
            {
                domainsQuery = domainsQuery.Where(d => d.ClientId == clientId);
            }

            // Apply sorting
            domainsQuery = sortOrder switch
            {
                "domain_asc" => domainsQuery.OrderBy(d => d.DomainName),
                "domain_desc" => domainsQuery.OrderByDescending(d => d.DomainName),
                "tld_asc" => domainsQuery.OrderBy(d => d.Tld),
                "tld_desc" => domainsQuery.OrderByDescending(d => d.Tld),
                "client_asc" => domainsQuery.OrderBy(d => d.Client != null ? d.Client.Title : string.Empty),
                "client_desc" => domainsQuery.OrderByDescending(d => d.Client != null ? d.Client.Title : string.Empty),
                "expiration_asc" => domainsQuery.OrderBy(d => d.ExpirationDate),
                "expiration_desc" => domainsQuery.OrderByDescending(d => d.ExpirationDate),
                "created_asc" => domainsQuery.OrderBy(d => d.DateCreated),
                "created_desc" => domainsQuery.OrderByDescending(d => d.DateCreated),
                _ => domainsQuery.OrderBy(d => d.DomainName)
            };

            // Get dropdown data for client filter
            ClientSelectList = new SelectList(
                await _context.Clients
                    .OrderBy(c => c.Title)
                    .Select(c => new { c.Id, c.Title })
                    .AsNoTracking()
                    .ToListAsync(),
                "Id", "Title");

            const int pageSize = 10;
            Domains = await PaginatedList<Domain>.CreateAsync(
                domainsQuery, pageIndex ?? 1, pageSize);            // Initialize data needed for pricing calculations
            var clients = await _context.Clients
                .Include(c => c.PriceList)
                .ToDictionaryAsync(c => c.Id, c => c);            // Handle possible duplicate TLDs by fetching all products first, then processing in memory
            var allProducts = await _context.Products
                .AsNoTracking()
                .ToListAsync();
                  // Process in memory to normalize TLDs and handle duplicates
            // Include both domain TLDs and service products (REGISTRY_LOCK, PRIVACY, etc.)
            var products = allProducts
                .Where(p => !string.IsNullOrEmpty(p.Tld))
                .GroupBy(p => p.Tld.ToUpperInvariant())
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            var priceListEntries = await _context.DomainPriceListEntries
                .Include(p => p.Product)
                .Include(p => p.PriceList)
                .ToListAsync();

            var clientDiscounts = await _context.ClientProductDiscounts
                .Include(d => d.Product)
                .ToListAsync();

            // Calculate renewal costs for all domains
            DomainRenewalCosts = new Dictionary<Guid, decimal>();
            
            // If we're in list view, calculate renewal costs for paginated domains
            if (!GroupByClient)
            {                foreach (var domain in Domains)
                {
                    // Skip if the domain is null
                    if (domain == null)
                    {
                        continue;
                    }                    // Skip if the domain has empty TLD
                    if (string.IsNullOrEmpty(domain.Tld))
                    {
                        DomainRenewalCosts[domain.Id] = 0m;
                        continue;
                    }

                    // Normalize TLD to uppercase for case-insensitive lookup
                    var normalizedTld = domain.Tld.ToUpperInvariant();
                    
                    // Check if this TLD exists in products dictionary
                    if (!products.ContainsKey(normalizedTld))
                    {
                        DomainRenewalCosts[domain.Id] = 0m;
                        continue;
                    }

                    var renewalCost = CalculateDomainRenewalCost(
                        domain,
                        clients.TryGetValue(domain.ClientId, out var client) ? client : null,
                        products,
                        priceListEntries,
                        clientDiscounts);

                    DomainRenewalCosts[domain.Id] = renewalCost;
                }
            }
            // If grouping by client is enabled
            else
            {                // Group domains by client
                var allDomains = await domainsQuery.ToListAsync();
                
                // Create the grouped domains dictionary
                GroupedDomains = allDomains
                    .GroupBy(d => d.ClientId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Store client names for easier access
                ClientNames = clients.ToDictionary(c => c.Key, c => c.Value.Title ?? "Unknown Client");

                // Calculate renewal costs for each domain in grouped view
                ClientTotalRenewalCosts = new Dictionary<Guid, decimal>();                foreach (var domain in allDomains)
                {
                    // Skip if the domain is null
                    if (domain == null)
                    {
                        continue;
                    }                    // Skip if the domain has empty TLD
                    if (string.IsNullOrEmpty(domain.Tld))
                    {
                        DomainRenewalCosts[domain.Id] = 0m;
                        
                        // Add to client total with zero cost
                        if (ClientTotalRenewalCosts.ContainsKey(domain.ClientId))
                        {
                            // No need to add 0
                        }
                        else
                        {
                            ClientTotalRenewalCosts[domain.ClientId] = 0m;
                        }
                        continue;
                    }

                    // Normalize TLD to uppercase for case-insensitive lookup
                    var normalizedTld = domain.Tld.ToUpperInvariant();
                    
                    // Check if this TLD exists in products dictionary
                    if (!products.ContainsKey(normalizedTld))
                    {
                        DomainRenewalCosts[domain.Id] = 0m;
                        
                        // Add to client total with zero cost
                        if (ClientTotalRenewalCosts.ContainsKey(domain.ClientId))
                        {
                            // No need to add 0
                        }
                        else
                        {
                            ClientTotalRenewalCosts[domain.ClientId] = 0m;
                        }
                        continue;
                    }

                    var renewalCost = CalculateDomainRenewalCost(
                        domain,
                        clients.TryGetValue(domain.ClientId, out var client) ? client : null,
                        products,
                        priceListEntries,
                        clientDiscounts);

                    DomainRenewalCosts[domain.Id] = renewalCost;

                    // Add to client total
                    if (ClientTotalRenewalCosts.ContainsKey(domain.ClientId))
                    {
                        ClientTotalRenewalCosts[domain.ClientId] += renewalCost;
                    }
                    else
                    {
                        ClientTotalRenewalCosts[domain.ClientId] = renewalCost;
                    }
                }
            }
        }        private decimal CalculateDomainRenewalCost(
            Domain domain,
            Client? client,
            Dictionary<string, Product> products,
            List<PriceListEntry> priceListEntries,
            List<ClientProductDiscount> clientDiscounts)
        {
            // Safety check for null domain or TLD
            if (domain == null || string.IsNullOrEmpty(domain.Tld))
            {
                return 0m; // Unknown TLD or null domain
            }

            // Normalize TLD to uppercase for case-insensitive lookup
            var normalizedTld = domain.Tld.ToUpperInvariant();
            if (!products.TryGetValue(normalizedTld, out var product))
            {
                return 0m; // Unknown TLD
            }

            // Initialize breakdown dictionary for this domain
            var breakdown = new Dictionary<string, decimal>();
            
            // Start with base price for domain registration
            decimal baseDomainPrice = product.BasePrice;

            // Apply price list if client has one for the base domain product
            if (client?.PriceListId != null)
            {
                var priceListEntry = priceListEntries
                    .FirstOrDefault(p => p.PriceListId == client.PriceListId && p.ProductId == product.Id);

                if (priceListEntry?.OutPrice != null)
                {
                    baseDomainPrice = priceListEntry.OutPrice.Value;
                }
            }

            // Apply client specific discount for the base domain product
            var clientDiscount = clientDiscounts
                .FirstOrDefault(d => d.ClientId == domain.ClientId && d.ProductId == product.Id);

            if (clientDiscount != null)
            {
                if (clientDiscount.FixedPrice.HasValue)
                {
                    // Fixed price overrides all other pricing
                    baseDomainPrice = clientDiscount.FixedPrice.Value;
                }
                else if (clientDiscount.Discount.HasValue)
                {
                    // Apply percentage discount
                    baseDomainPrice = baseDomainPrice * (1 - (clientDiscount.Discount.Value / 100));
                }
            }

            // Store the base domain price in the breakdown
            breakdown["Base Domain"] = baseDomainPrice;
            
            // Track total price
            decimal totalPrice = baseDomainPrice;

            // Handle Registry Lock if enabled
            if (domain.HasRegistryLock)
            {
                decimal registryLockPrice = GetAdditionalServicePrice(
                    "REGISTRY_LOCK", // Product code for registry lock
                    client,
                    products,
                    priceListEntries,
                    clientDiscounts,
                    domain.ClientId);
                    
                breakdown["Registry Lock"] = registryLockPrice;
                totalPrice += registryLockPrice;
            }

            // Handle Domain Privacy if enabled
            if (domain.HasPrivacy)
            {
                decimal privacyPrice = GetAdditionalServicePrice(
                    "PRIVACY", // Product code for privacy
                    client,
                    products,
                    priceListEntries,
                    clientDiscounts,
                    domain.ClientId);
                    
                breakdown["Privacy"] = privacyPrice;
                totalPrice += privacyPrice;
            }

            // Handle Local Presence if enabled
            if (domain.HasLocalPresence)
            {
                decimal localPresencePrice = GetAdditionalServicePrice(
                    "LOCAL_PRESENCE", // Product code for local presence
                    client,
                    products,
                    priceListEntries,
                    clientDiscounts,
                    domain.ClientId);
                    
                breakdown["Local Presence"] = localPresencePrice;
                totalPrice += localPresencePrice;
            }

            // Handle Proxy if enabled
            if (domain.HasProxy)
            {
                decimal proxyPrice = GetAdditionalServicePrice(
                    "PROXY", // Product code for proxy service
                    client,
                    products,
                    priceListEntries,
                    clientDiscounts,
                    domain.ClientId);
                    
                breakdown["Proxy"] = proxyPrice;
                totalPrice += proxyPrice;
            }

            // Store the renewal period in the breakdown
            int renewPeriod = domain.RenewPeriod ?? 1;
            breakdown["Renewal Period"] = renewPeriod;
            
            // Store the total (before multiplying by period) in the breakdown
            breakdown["Subtotal"] = totalPrice;
            
            // Store the final total (after multiplying by period) in the breakdown
            breakdown["Total"] = totalPrice * renewPeriod;
            
            // Store the breakdown in the dictionary
            if (DomainRenewalCostBreakdown.ContainsKey(domain.Id))
            {
                DomainRenewalCostBreakdown[domain.Id] = breakdown;
            }
            else
            {
                DomainRenewalCostBreakdown.Add(domain.Id, breakdown);
            }

            // Multiply by renewal period if set
            return totalPrice * renewPeriod;
        }

        private decimal GetAdditionalServicePrice(
            string productCode,
            Client? client,
            Dictionary<string, Product> products,
            List<PriceListEntry> priceListEntries,
            List<ClientProductDiscount> clientDiscounts,
            Guid clientId)
        {
            // Try to find the product by its code (using the dictionary to avoid case issues)
            var serviceProduct = products.Values.FirstOrDefault(p => 
                !string.IsNullOrEmpty(p.Tld) && 
                p.Tld.ToUpperInvariant() == productCode);

            if (serviceProduct == null)
            {
                return 0m; // Product not found
            }

            // Start with base price
            decimal price = serviceProduct.BasePrice;

            // Apply price list if client has one
            if (client?.PriceListId != null)
            {
                var priceListEntry = priceListEntries
                    .FirstOrDefault(p => p.PriceListId == client.PriceListId && p.ProductId == serviceProduct.Id);

                if (priceListEntry?.OutPrice != null)
                {
                    price = priceListEntry.OutPrice.Value;
                }
            }

            // Apply client specific discount if any
            var clientDiscount = clientDiscounts
                .FirstOrDefault(d => d.ClientId == clientId && d.ProductId == serviceProduct.Id);

            if (clientDiscount != null)
            {
                if (clientDiscount.FixedPrice.HasValue)
                {
                    // Fixed price overrides all other pricing
                    price = clientDiscount.FixedPrice.Value;
                }
                else if (clientDiscount.Discount.HasValue)
                {
                    // Apply percentage discount
                    price = price * (1 - (clientDiscount.Discount.Value / 100));
                }
            }

            return price;
        }
    }
}
