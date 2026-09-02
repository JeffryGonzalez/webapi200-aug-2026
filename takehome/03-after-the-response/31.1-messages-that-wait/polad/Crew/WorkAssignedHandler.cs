using Practice.Contracts;

namespace Crew;

public static class WorkAssignedHandler
{
    public static void Handle(WorkAssigned message, ILogger<WorkAssigned> logger)
    {
        logger.LogInformation("Crew {Crew} has {Number} at {Location}",
            message.Crew, message.Number, message.Location);
    }
}
