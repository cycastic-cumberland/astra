using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Astra.Engine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Astra.Server;

public class TcpServer : IDisposable
{
    public const int DefaultPort = 8488;
    private const string ConfigPathEnvEntry = "ASTRA_CONFIG_PATH";
    private static readonly byte[] FaultedResponse = { 1 };

    private static readonly IReadOnlyDictionary<string, LogLevel> StringToLog = new Dictionary<string, LogLevel>
    {
        ["Trace"] = LogLevel.Trace,
        ["Information"] = LogLevel.Information,
        ["Debug"] = LogLevel.Debug,
        ["Warning"] = LogLevel.Warning,
        ["Error"] = LogLevel.Error,
        ["Critical"] = LogLevel.Critical,
        ["None"] = LogLevel.None,
        
        ["trace"] = LogLevel.Trace,
        ["information"] = LogLevel.Information,
        ["debug"] = LogLevel.Debug,
        ["warning"] = LogLevel.Warning,
        ["error"] = LogLevel.Error,
        ["critical"] = LogLevel.Critical,
        ["none"] = LogLevel.None,
        
        ["TRACE"] = LogLevel.Trace,
        ["INFORMATION"] = LogLevel.Information,
        ["DEBUG"] = LogLevel.Debug,
        ["WARNING"] = LogLevel.Warning,
        ["ERROR"] = LogLevel.Error,
        ["CRITICAL"] = LogLevel.Critical,
        ["NONE"] = LogLevel.None,
    };
    private static readonly IPAddress Address = IPAddress.Parse("0.0.0.0");
    private static readonly IPGlobalProperties IpProperties = IPGlobalProperties.GetIPGlobalProperties();
    private readonly DataIndexRegistry _registry;
    private readonly TcpListener _listener;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TcpServer> _logger;
    private readonly int _port;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public int Port => _port;

    public ILogger<TL> GetLogger<TL>() => _loggerFactory.CreateLogger<TL>();
    
#if DEBUG
    public DataIndexRegistry ProbeRegistry() => _registry;
#endif
    
    public TcpServer(AstraLaunchSettings settings)
    {
        var logLevel = StringToLog.GetValueOrDefault(settings.LogLevel ?? "Information", LogLevel.Information);
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(logLevel);
            builder.AddSpringBootLoggerClone(configure =>
            {
                configure.ColoredOutput = true;
            });
        });
        _registry = new(settings.Schema, _loggerFactory);
        _logger = _loggerFactory.CreateLogger<TcpServer>();
        _port = DefaultPort;
        _listener = new(Address, _port);
        Console.CancelKeyPress += delegate(object? _, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Kill();
        };
        _logger.LogInformation("Initialization completed: {} = {}, {} = {}, {} = {}, {} = {})",
             nameof(settings.LogLevel), logLevel.ToString(), 
             nameof(_registry.ColumnCount), _registry.ColumnCount, 
            nameof(_registry.IndexedColumnCount), _registry.IndexedColumnCount, 
            nameof(_registry.ReferenceTypeColumnCount), _registry.ReferenceTypeColumnCount);
    }

    public static async Task<TcpServer?> Initialize()
    {
        SpringBootLoggerClone.PrintBanner();
        using var tmpLogger = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Error);
            builder.AddSpringBootLoggerClone(configure =>
            {
                configure.ColoredOutput = true;
            });
        });
        var logger = tmpLogger.CreateLogger<TcpServer>();
        var configPath = Environment.GetEnvironmentVariable(ConfigPathEnvEntry);
        if (configPath == null)
        {
            logger.LogError("Configuration path not specified, please pass it using the '{}' environment variable",
                ConfigPathEnvEntry);
            return null;
        }

        if (!File.Exists(configPath))
        {
            logger.LogError("File '{}' does not exist", configPath);
            return null;
        }

        var configContent = await File.ReadAllTextAsync(configPath);
        var config = JsonConvert.DeserializeObject<AstraLaunchSettings>(configContent);
        if (config.Schema.Columns != null!) return new TcpServer(config);
        logger.LogError("File '{}' does not follow the correct format", configPath);
        return null;
    }

    private static bool IsConnected(TcpClient client)
    {
        var tcpConnection = IpProperties
            .GetActiveTcpConnections()
            .FirstOrDefault(x => x.LocalEndPoint.Equals(client.Client.LocalEndPoint) &&
                                 x.RemoteEndPoint.Equals(client.Client.RemoteEndPoint));
        var stateOfConnection = tcpConnection?.State;
        return stateOfConnection == TcpState.Established;
    }
    
    private async Task ResolveClientAsync(TcpClient client)
    {
        var cancellationToken = _cancellationTokenSource.Token;
        // Send info regarding endianness
        // Checking endianness:
        // {
        //     using var checkEndianness = BytesCluster.Rent(4);
        //     await stream.ReadAsync(checkEndianness.WriterMemory);
        //     var isLittleEndian = checkEndianness.Reader[0] == 1;
        // }
        var stream = client.GetStream();
        await stream.WriteValueAsync(1, cancellationToken);
        var stopwatch = new Stopwatch();
        var threshold = (long)sizeof(long);
        var waiting = true;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (client.Available < threshold)
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#endif
                continue;
            }
            if (waiting)
            {
                threshold = await stream.ReadLongAsync(cancellationToken);
                waiting = false;
                continue;
            }
            stopwatch.Start();
            var cluster = BytesCluster.Rent((int)threshold);

            _ = await stream.ReadAsync(cluster.WriterMemory, cancellationToken);

            await using var readStream = cluster.Promote();
            await using var writeStream = MemoryStreamPool.Allocate();
            try
            {
                _registry.ConsumeStream(readStream, writeStream);
                await stream.WriteValueAsync(writeStream.Length, token: cancellationToken);
                await stream.WriteAsync(new ReadOnlyMemory<byte>(writeStream.GetBuffer(), 0,
                    (int)writeStream.Length), cancellationToken);
            }
            catch (Exception e)
            {
                // "Faulted" response
                await writeStream.WriteValueAsync(1L, token: cancellationToken);
                await writeStream.WriteAsync(FaultedResponse, cancellationToken);
                _logger.LogError(e, "Error occured while resolving request");
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogDebug("Request resolved after {} us", stopwatch.Elapsed.TotalMicroseconds);
                stopwatch.Reset();
            }
            threshold = sizeof(long);
            waiting = false;
        }
    }
    
    private async Task ResolveClientWrappedAsync(TcpClient client)
    {
        var addr = Address;
        int port = ushort.MaxValue;
        if (client.Client.RemoteEndPoint != null)
        {
            addr = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
            port = ((IPEndPoint)client.Client.RemoteEndPoint).Port;
        }

        try
        {
            _logger.LogDebug("Connection from {}:{} opened", addr, port);
            await ResolveClientAsync(client);
            _logger.LogDebug("Connection from {}:{} closed", addr, port);
        }
        catch (SocketException)
        {
            _logger.LogDebug("Connection from {}:{} closed disruptively", addr, port);
        }
        catch (TaskCanceledException)
        {
            
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occured while handling request from {}:{}", addr, port);
        }
        finally
        {
            client.Close();
        }
    }

    public void Kill()
    {
        _cancellationTokenSource.Cancel();
    }
    
    public async Task RunAsync()
    {
        _listener.Start();
        _logger.LogInformation("Astra.Server is listening to port {}", _port);
        var token = _cancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
#pragma warning disable CS4014
                Task.Run(() => ResolveClientWrappedAsync(client), _cancellationTokenSource.Token);
#pragma warning restore CS4014
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
        }
        _logger.LogInformation("Astra.Server is shutting down");
        Dispose();
    }

    public void Run()
    {
        RunAsync().Wait();
    }

    public void Dispose()
    {
        _listener.Stop();
        _listener.Dispose();
        _loggerFactory.Dispose();
    }
}