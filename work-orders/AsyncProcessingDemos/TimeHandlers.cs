namespace AsyncProcessingDemos;

public class TimeHandler
{
    public  async Task Handle(WaitCommand command, ILogger<TimeHandler> logger)
    {
        await Task.Delay(command.Seconds * 1000);
        logger.LogInformation("Work completed after {Seconds} seconds.", command.Seconds);
    } 
}


