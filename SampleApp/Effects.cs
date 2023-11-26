namespace SampleApp;

public class Effects
{
    public Task Process(Action message)
    {
        Console.WriteLine($"Effect:{message}");
        return Task.CompletedTask;
    }


}
