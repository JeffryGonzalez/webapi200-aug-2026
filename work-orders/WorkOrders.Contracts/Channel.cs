namespace WorkOrders.Contracts;

/// <summary>
/// The four ways a work order reaches Streets. Three of them are not automated, which
/// is the problem the village contracted to fix.
/// </summary>
public enum Channel
{
    WebsiteForm,
    SharedMailbox,
    Phone,
    Clipboard
}
