using Practice.Contracts;

namespace Notifications;

public static class NotificationHandler
{
    public static void Handle(WorkAssigned message, ILogger<WorkAssigned> logger)
    {
        logger.LogInformation("NOTIFIED resident: {Number} assigned to {Crew}",
            message.Number, message.Crew);
    }

    public static void Handle(ShiftNoteAdded note, ILogger<ShiftNoteAdded> logger)
    {
        logger.LogInformation("WALLBOARD {Crew}: {Note}", note.Crew, note.Note);
    }
}
