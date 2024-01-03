namespace Astra.Server;

public static class Program
{
    public static async Task Main()
    {
        // var parsedArguments = CommandLineArgumentsParser.Parse(args);
        var server = await TcpServer.Initialize();
        if (server == null)
        {
            Environment.Exit(1);
        }
        await server.RunAsync();
    }
}