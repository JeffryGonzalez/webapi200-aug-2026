using Marten;
using Marten.Schema;
using WorkOrders.Contracts;

namespace WorkOrders.Api;

/// <summary>
/// The work orders as they stand on the morning the contractors arrive.
///
/// This is the forwarded thread, in the database. Nothing announces the duplicate and
/// nothing announces the dispatch date.
/// </summary>
public class WorkOrderSeed : IInitialData
{
    static Guid Id(string s) => new(System.Security.Cryptography.MD5.HashData(
        System.Text.Encoding.UTF8.GetBytes(s)));

    public async Task Populate(IDocumentStore store, CancellationToken cancellation)
    {
        await using var session = store.LightweightSession();

        if (await session.Query<WorkOrder>().AnyAsync(cancellation)) return;

        session.Store(
            // Harold Mink, through the website form. The one in the thread.
            new WorkOrder
            {
                Id = Id("wo-2026-0817"), Number = "2026-0817",
                Channel = Channel.WebsiteForm, Status = WorkOrderStatus.Dispatched,
                ReportedBy = "Harold Mink",
                Location = "Depot St, between N. Salyer and the alley behind the old feed store",
                Description = "Pothole approx. 14 inches across, eastbound lane, approx. 40 ft past the manhole cover. Photographs attached (4).",
                ReportedOn = new(2026, 8, 27),
                // Dispatched to Kerns in August. Kerns was suspended in May.
                DispatchedTo = "Kerns Excavating",
                DispatchedOn = new(2026, 8, 25)
            },

            // The same hole, reported by phone, taken down by somebody at Village Hall.
            // Nothing links it to 0817.
            new WorkOrder
            {
                Id = Id("wo-2026-0818"), Number = "2026-0818",
                Channel = Channel.Phone, Status = WorkOrderStatus.Open,
                ReportedBy = "caller did not give name",
                Location = "Depot Street near the feed store",
                Description = "Large hole in the road. Caller says a school bus hit it twice.",
                ReportedOn = new(2026, 8, 27)
            },

            // A genuinely different hole. Ted says so in the thread.
            new WorkOrder
            {
                Id = Id("wo-2026-0819"), Number = "2026-0819",
                Channel = Channel.WebsiteForm, Status = WorkOrderStatus.Open,
                ReportedBy = "Harold Mink",
                Location = "N. Salyer at the culvert crossing",
                Description = "Second location, different from the Depot St one. Edge of pavement broken away.",
                ReportedOn = new(2026, 8, 28)
            },

            new WorkOrder
            {
                Id = Id("wo-2026-0803"), Number = "2026-0803",
                Channel = Channel.SharedMailbox, Status = WorkOrderStatus.Closed,
                ReportedBy = "r.amankwah@theoria.oh.gov",
                Location = "Water & Sewer yard, gate latch",
                Description = "Gate will not latch. Not urgent.",
                ReportedOn = new(2026, 8, 11)
            },

            new WorkOrder
            {
                Id = Id("wo-2026-0811"), Number = "2026-0811",
                Channel = Channel.Clipboard, Status = WorkOrderStatus.Closed,
                ReportedBy = "T. Vosmik",
                Location = "Shelterhouse lot, Parks",
                Description = "Sign post bent. Straightened it.",
                ReportedOn = new(2026, 8, 19)
            });

        await session.SaveChangesAsync(cancellation);
    }
}
