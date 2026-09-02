using System.ComponentModel.DataAnnotations;

namespace Orders;

public record NewWorkOrder
{
    [Required, MinLength(3)]
    public string Department { get; init; } = "";

    [Required, MinLength(10)]
    public string Description { get; init; } = "";
}
