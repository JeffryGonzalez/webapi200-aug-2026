using WorkOrders.Contracts;

namespace WorkOrders.Api;

public enum WorkOrderStatus { Open, Dispatched, Closed }

public class WorkOrder
{
    public Guid Id { get; set; }

    /// <summary>The number people actually say out loud. "2026-0817".</summary>
    public string Number { get; set; } = "";

    public Channel Channel { get; set; }
    public WorkOrderStatus Status { get; set; }

    public string ReportedBy { get; set; } = "";
    public string Location { get; set; } = "";
    public string Description { get; set; } = "";

    public DateOnly ReportedOn { get; set; }

    /// <summary>Set when work goes out. Nothing checks vendor standing first.</summary>
    public string? DispatchedTo { get; set; }
    public DateOnly? DispatchedOn { get; set; }
}
