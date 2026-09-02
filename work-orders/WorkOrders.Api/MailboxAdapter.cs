using Marten;
using WorkOrders.Contracts;

namespace WorkOrders.Api;

/// <summary>
/// Polls the shared mailbox and turns messages into work orders.
///
/// Written by the previous contractor. It works, mostly.
/// </summary>
public class MailboxAdapter(IServiceProvider services, ILogger<MailboxAdapter> logger)
    : BackgroundService
{
    // Stands in for the mailbox. A real one would be IMAP; this is the same shape.
    private static readonly Queue<MailboxMessage> Inbox = new(); // Queue is not thread-safe. 
    // You should use either a ConcurrentQueue, use locking,  or a Channel for a real implementation.
    // Channel is awesome - but still wouldn't use it for work like this.

    public static void Deliver(MailboxMessage message) => Inbox.Enqueue(message);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            if (Inbox.Count == 0) continue;

            var message = Inbox.Dequeue();
            Inbox.Clear(); // Notice this - yuck. We'll talk about that. 

            await using var scope = services.CreateAsyncScope();
            var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            var order = new WorkOrder
            {
                Id = Guid.CreateVersion7(),
                Number = await Numbering.NextAsync(session, stoppingToken),
                Channel = Channel.SharedMailbox,
                Status = WorkOrderStatus.Open,
                ReportedBy = message.From,
                Location = message.Subject,
                Description = message.Body,
                ReportedOn = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            session.Store(order);
            await session.SaveChangesAsync(stoppingToken);

            logger.LogInformation("Mailbox: created {Number} from {From}", order.Number, message.From);
        }
    }
}

public record MailboxMessage(string From, string Subject, string Body);
