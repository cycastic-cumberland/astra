using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine;
using Astra.Engine.Data;
using Astra.Engine.v2.Data;
using Astra.Server.Authentication;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Astra.Server;

public class TcpServer : IDisposable
{
    public const int DefaultPort = 8488;
    private const string ConfigPathEnvEntry = "ASTRA_CONFIG_PATH";
    private static readonly byte[] FaultedResponse = [ 1 ];
    private static readonly byte[] SafeResponse = [ 0 ];

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
    private readonly IRegistry _registry;
    private readonly TcpListener _listener;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TcpServer> _logger;
    private readonly int _port;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Func<IAuthenticationHandler> _authenticationSpawner;
    private readonly int _timeout;

    public int Port => _port;

    public ILogger<TL> GetLogger<TL>() => _loggerFactory.CreateLogger<TL>();
    
    public IRegistry GetRegistry() => _registry;
    
    public TcpServer(AstraLaunchSettings settings, Func<IAuthenticationHandler> authenticationSpawner)
    {
        _timeout = settings.Timeout <= 0 ? 100_000 : settings.Timeout;
        _authenticationSpawner = authenticationSpawner;
        var logLevel = StringToLog.GetValueOrDefault(settings.LogLevel ?? "Information", LogLevel.Information);
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(logLevel);
            builder.AddSpringBootLoggerClone(configure =>
            {
                configure.ColoredOutput = true;
            });
        });
        if (settings.UseCellBasedDataStore)
            _registry = new ShinDataRegistry(settings.Schema, _loggerFactory);
        else
            _registry = new DataRegistry(settings.Schema, _loggerFactory);
        _logger = GetLogger<TcpServer>();
        _port = settings.Port ?? DefaultPort;
        _listener = new(Address, _port);
        Console.CancelKeyPress += delegate(object? _, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Kill();
        };
        _logger.LogInformation("Initialization completed: {} = {}, {} = {}, {} = {}, {} = {}, {} = {})",
                nameof(settings.LogLevel), logLevel.ToString(), 
                nameof(_registry.ColumnCount), _registry.ColumnCount, 
                nameof(_registry.IndexedColumnCount), _registry.IndexedColumnCount, 
                nameof(settings.AuthenticationMethod), settings.AuthenticationMethod,
                nameof(settings.Timeout), TimeSpan.FromMilliseconds(_timeout));
    }

    public static async Task<TcpServer?> Initialize()
    {
        SpringBootLoggerClone.PrintBanner();
        using var tmpLogger = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
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
        AstraLaunchSettings config;
        try
        {
            config = JsonConvert.DeserializeObject<RepresentableAstraLaunchSettings>(configContent).ToInternal();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception occured while deserializing launch settings");
            return null;
        }
        Func<IAuthenticationHandler> authSpawner;
        switch (config.AuthenticationMethod)
        {
            case CommunicationProtocol.NoAuthentication:
                authSpawner = () => new NoAuthenticationHandler();
                break;
            case CommunicationProtocol.PasswordAuthentication:
            {
                logger.LogError("{} is deprecated, please use {}", 
                    nameof(CommunicationProtocol.PasswordAuthentication),
                    nameof(CommunicationProtocol.SaltedPasswordAuthentication));
                return null;
            }
            case CommunicationProtocol.SaltedPasswordAuthentication:
            {
                if (string.IsNullOrEmpty(config.Password))
                {
                    logger.LogError("Password authentication required but no password provided");
                    return null;
                }

                var timeout = config.Timeout;
                authSpawner = AuthenticationHelper.SaltedSha256Authentication(config.Password, timeout);
                break;
            }
            case CommunicationProtocol.PubKeyAuthentication:
            {
                if (string.IsNullOrEmpty(config.PublicKeyPath))
                {
                    logger.LogError("Public key authentication required but no public key path specified");
                    return null;
                }

                if (!File.Exists(config.PublicKeyPath))
                {
                    logger.LogError("Public key path is invalid");
                    return null;
                }

                var pubKey = await File.ReadAllTextAsync(config.PublicKeyPath);
                
                var timeout = config.Timeout;

                try
                {
                    authSpawner = () => new PublicKeyAuthenticationHandler(pubKey, timeout);
                    using var testSubject = new PublicKeyAuthenticationHandler(pubKey, timeout);
                }
                catch (CryptographicException)
                {
                    logger.LogError("Provided public key is invalid");
                    return null;
                }
                break;
            }
            default:
            {
                logger.LogError("Authentication method not supported {}", config.AuthenticationMethod);
                return null;
            }
        }
        return new TcpServer(config, authSpawner);
    }

    private static bool IsConnected(TcpClient client)
    {
        try
        {
            var tcpConnection = IpProperties
                .GetActiveTcpConnections()
                .FirstOrDefault(x => x.LocalEndPoint.Equals(client.Client.LocalEndPoint) &&
                                     x.RemoteEndPoint.Equals(client.Client.RemoteEndPoint));
            var stateOfConnection = tcpConnection?.State;
            return stateOfConnection == TcpState.Established;
        }
        catch (Exception)
        {
            return false;
        }
    }


    private async Task ResolveClientAsync(TcpClient client)
    {
        var cancellationToken = _cancellationTokenSource.Token;
        var stream = client.GetStream();
        client.NoDelay = true;
        using var bufferedStream = new WriteForwardBufferedStream(stream);
        var stopwatch = new Stopwatch();
        var threshold = (long)sizeof(long);
        var waiting = true;
        var lastCheck = 0L;
        var timer = Stopwatch.StartNew();
        while (!cancellationToken.IsCancellationRequested)
        {
            var elapsed = timer.ElapsedMilliseconds;
            if (elapsed - lastCheck > 60_000)
            {
                lastCheck = elapsed;
                if (!IsConnected(client)) return;
            }
            if (client.Available < threshold)
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#else
                Thread.Yield();
#endif
                if (!waiting && elapsed > _timeout)
                {
                    _logger.LogInformation("Client timed out");
                    return;
                }
                continue;
            }
            if (waiting)
            {
                _ = await stream.ReadLongAsync(cancellationToken);
                threshold = 0;
                waiting = false;
                timer.Restart();
                lastCheck = 0;
                continue;
            }
            stopwatch.Start();
            try
            {
                _registry.ConsumeStream(stream, bufferedStream);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occured while resolving request");
                client.Close();
                return;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogDebug("Request resolved after {} us", stopwatch.Elapsed.TotalMicroseconds);
                bufferedStream.Flush();
                stopwatch.Reset();
            }
            threshold = sizeof(long);
            waiting = true;
            timer.Restart();
        }
    }
    
    // Handshake procedure:
    // 1. Send a 32-bit integer to check for endianness
    // 2. Send a 64-bit integer as identification
    // 3. Send the version of this server to the client
    // 4. Wait for the client to send back another corresponding 64-bit integer to complete handshake
    // 5. If the integer does not match, stop communication
    // 6. Send authentication instruction and continue
    private async Task AuthenticateClient(TcpClient client, IPAddress address)
    {
        var cancellationToken = _cancellationTokenSource.Token;
        var stream = client.GetStream();
        try
        {
            await stream.WriteValueAsync(1, cancellationToken);
            await stream.WriteValueAsync(CommunicationProtocol.ServerIdentification, cancellationToken);
            await stream.WriteValueAsync(CommonProtocol.AstraCommonVersion, token: cancellationToken);
            var timer = Stopwatch.StartNew();
            while (client.Available < sizeof(ulong))
            {
#if DEBUG
                await Task.Delay(100, cancellationToken);
#else
                Thread.Yield();
#endif
                if (timer.ElapsedMilliseconds <= _timeout) continue;
                _logger.LogInformation("Client {} failed handshake attempt: timed out", address);
                client.Close();
                return;
            }

            var handshakeAttempt = await stream.ReadULongAsync(token: cancellationToken);
            if (handshakeAttempt != CommunicationProtocol.SimpleClientResponse)
            {
                _logger.LogDebug("Client {} failed handshake attempt: incorrect message", address);
                client.Close();
                return;
            }
            using var authHandler = _authenticationSpawner();
            var authResult = await authHandler.Authenticate(client, cancellationToken);
            if (authResult != IAuthenticationHandler.AuthenticationState.AllowConnection)
            {
                await stream.WriteValueAsync(CommunicationProtocol.RejectedConnection, token: cancellationToken);
                var reason = authResult switch
                {
                    IAuthenticationHandler.AuthenticationState.Timeout => "timed out",
                    IAuthenticationHandler.AuthenticationState.RejectConnection => "authentication failed",
                    _ => "<unknown>"
                };
                _logger.LogDebug("Authentication for client {} was rejected: {}", address, reason);
                client.Close();
                return;
            }
        }
        catch (Exception)
        {
            client.Close();
            throw;
        }
        _logger.LogDebug("Authentication completed, {} is allowed to connect", address);
        await stream.WriteValueAsync(CommunicationProtocol.AllowedConnection, token: cancellationToken);
        await ResolveClientAsync(client);
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
            // await ResolveClientAsync(client);
            await AuthenticateClient(client, addr);
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
            catch (TaskCanceledException)
            {
                // Ignored
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