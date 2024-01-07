using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Astra.Server;

public class Program
{
    public static async Task Main()
    {
        // var parsedArguments = CommandLineArgumentsParser.Parse(args);
        var timer = Stopwatch.StartNew();
        var server = await TcpServer.Initialize();
        if (server == null)
        {
            Environment.Exit(1);
        }

        var initTime = timer.ElapsedMilliseconds;
        server.GetLogger<Program>().LogDebug("Initialization finished after {} ms", initTime);
        await server.RunAsync();
    }
}