using System.ServiceProcess;

// In .NET 10, ensure you've added the NuGet package:
// dotnet add package System.ServiceProcess.ServiceController

var serviceName = args.Length > 0 ? args[0] : "pgbouncer";

try
{
    using var sc = new ServiceController(serviceName);

    // 1. Display current details
    Console.WriteLine("-------------------------");
    Console.WriteLine($"--- {sc.DisplayName} ---");
    Console.WriteLine($"Status:  {sc.Status}");
    Console.WriteLine($"CanStop: {sc.CanStop}");
    Console.WriteLine("-------------------------");

    // 2. Determine options based on state
    if (sc.Status == ServiceControllerStatus.Running)
    {
        Console.Write("\nChoose an action: [S]top, [R]estart, or [Q]uit: ");
        var choice = Console.ReadKey().KeyChar.ToString().ToUpper();
        Console.WriteLine(); // New line for formatting

        switch (choice)
        {
            case "S":
                await HandleStop(sc);
                break;
            case "R":
                await HandleRestart(sc);
                break;
            default:
                Console.WriteLine("Exiting without changes.");
                break;
        }
    }
    else if (sc.Status == ServiceControllerStatus.Stopped)
    {
        Console.Write("\nService is stopped. [S]tart or [Q]uit? ");
        if (Console.ReadKey().KeyChar.ToString().ToUpper() == "S")
        {
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
            Console.WriteLine("\nService started successfully.");
        }
    }
}
catch (InvalidOperationException)
{
    Console.WriteLine($"\n[Error] Service '{serviceName}' not found. Try running as Admin.");
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("\n[Error] Admin privileges required to manage services.");
}

async Task HandleStop(ServiceController sc)
{
    if (!sc.CanStop)
    {
        Console.WriteLine("This service is flagged as un-stoppable.");
        return;
    }

    Console.WriteLine("Stopping service...");
    sc.Stop();

    // .NET 10 still uses the synchronous WaitForStatus, 
    // but we wrap the logic in a Task for better app responsiveness
    await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
    Console.WriteLine("Service stopped.");
}

async Task HandleRestart(ServiceController sc)
{
    Console.WriteLine("Restarting...");
    sc.Stop();
    await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
    sc.Start();
    await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
    Console.WriteLine("Service restarted.");
}