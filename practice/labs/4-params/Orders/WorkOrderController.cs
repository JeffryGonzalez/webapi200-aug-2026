using Microsoft.AspNetCore.Mvc;

namespace Orders;

public class WorkOrderController : ControllerBase
{
    [HttpGet("/work-orders2")]
    public ActionResult GetWorkOrders(int? page, string? department, WorkOrders orders)
    {
        if(page is null)
        {
            return BadRequest("Missing page parameter");
        }
        var results = orders.Page(page.Value, department);
        return Ok(results);
    }
}


/*
 * 
app.MapGet("/work-orders", (int page, string? department, WorkOrders orders) =>
{
    var results = orders.Page(page, department);
    return Results.Ok(results);
});
*/