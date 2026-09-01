namespace Orders;

public record WorkOrder(string Id, string Department, string Description, string Status);

/// <summary>
/// The work orders, in memory. Registered as a singleton, so it is shared by every
/// request and has to be safe to touch from more than one at a time.
/// </summary>
public class WorkOrders
{
    public const int PageSize = 5;

    private readonly Lock _gate = new(); // WTH?
    private int _nextNumber = 214;

    private readonly List<WorkOrder> _all =
    [
        new("2026-0201", "STR", "Pothole on Depot St, eastbound lane", "open"),
        new("2026-0202", "STR", "Streetlight out at Depot and Sixth", "open"),
        new("2026-0203", "SAN", "Missed collection, Kossuth Ave", "closed"),
        new("2026-0204", "PRK", "Bench slats broken, Vestibule Park", "open"),
        new("2026-0205", "STR", "Stop sign leaning at Third and Miami", "closed"),
        new("2026-0206", "WTR", "Hydrant weeping, 400 block of Canal", "open"),
        new("2026-0207", "SAN", "Cart not returned after collection", "closed"),
        new("2026-0208", "STR", "Crosswalk paint worn through at the school", "open"),
        new("2026-0209", "PRK", "Drinking fountain not running", "open"),
        new("2026-0210", "WTR", "Meter pit lid sitting proud of the sidewalk", "open"),
        new("2026-0211", "STR", "Pothole on Depot St, eastbound lane", "open"),
        new("2026-0212", "CLK", "Records room shelving unit pulling from the wall", "open"),
        new("2026-0213", "SAN", "Bulk pickup not collected, Harrison Ct", "open"),
    ];

    /// <summary>
    /// One page of work orders, optionally narrowed to a single department.
    /// A null department means "no filter". An empty one does not.
    /// </summary>
    public IReadOnlyList<WorkOrder> Page(int page, string? department)
    {
        // Isolated - no other request can access this while one thread is accessing it. 
      
        lock (_gate)
        {
            IEnumerable<WorkOrder> matching = _all;

            if (department is not null)
            {
                matching = matching.Where(w => w.Department == department);
            }

            return matching
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    }

    public WorkOrder Add(string department, string description)
    {
        lock (_gate)
        {
            var created = new WorkOrder($"2026-0{_nextNumber++}", department, description, "open");
            _all.Add(created);
            // what if an exceptionis thrown? What if this takes WAYYYY too long for whatever reason?
            
            return created;
        }
    }
}
