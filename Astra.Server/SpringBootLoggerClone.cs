using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Astra.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Configuration;

namespace Astra.Server;

public sealed class SpringBootLoggerCloneConfiguration
{
    public int EventId { get; set; }
    public bool ColoredOutput { get; set; }

    public Dictionary<LogLevel, ConsoleColor> LogLevelToColorMap { get; set; } = new()
    {
        [LogLevel.Trace] = ConsoleColor.Blue,
        [LogLevel.Information] = ConsoleColor.Green,
        [LogLevel.Debug] = ConsoleColor.Green,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Critical] = ConsoleColor.DarkRed,
        [LogLevel.None] = ConsoleColor.Gray,
    };
}

public sealed class SpringBootLoggerClone(
    string name,
    Func<SpringBootLoggerCloneConfiguration> getCurrentConfig)
    : ILogger
{
    private static readonly object Lock = new();

    private const string Banner = 
        """
             ___           ___           ___           ___           ___     
            /\  \         /\  \         /\  \         /\  \         /\  \    
           /::\  \       /::\  \        \:\  \       /::\  \       /::\  \   
          /:/\:\  \     /:/\ \  \        \:\  \     /:/\:\  \     /:/\:\  \  
         /::\~\:\  \   _\:\~\ \  \       /::\  \   /::\~\:\  \   /::\~\:\  \ 
        /:/\:\ \:\__\ /\ \:\ \ \__\     /:/\:\__\ /:/\:\ \:\__\ /:/\:\ \:\__\
        \/__\:\/:/  / \:\ \:\ \/__/    /:/  \/__/ \/_|::\/:/  / \/__\:\/:/  /
             \::/  /   \:\ \:\__\     /:/  /         |:|::/  /       \::/  / 
             /:/  /     \:\/:/  /     \/__/          |:|\/__/        /:/  /  
            /:/  /       \::/  /                     |:|  |         /:/  /   
            \/__/         \/__/                       \|__|         \/__/    
        """;

    public static void PrintBanner()
    {
        var originalColor = Console.ForegroundColor;
        Console.WriteLine();
        Console.WriteLine(Banner);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  :: Astra.Server ::");
        Console.ForegroundColor = originalColor;
        Console.Write("                                ");
        Console.WriteLine($"(v{CommonProtocol.GetCommonVersionString()})");
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;
    
    public bool IsEnabled(LogLevel logLevel) =>
        getCurrentConfig().LogLevelToColorMap.ContainsKey(logLevel);

    private static string StringifyLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            LogLevel.None => "NONE",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
    }

    private static string WrapName(string str, int maxLength)
    {
        if (str.Length < maxLength) return str + new string(' ', maxLength - str.Length);
        return "..." + str[(str.Length - maxLength + 3)..str.Length];
    }
    
    
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var config = getCurrentConfig();
        if (config.EventId != 0 && config.EventId != eventId.Id) return;

        var time = DateTime.Now;

        if (config.ColoredOutput)
            Task.Run(() =>
            {
                lock (Lock)
                {
                    var originalColor = Console.ForegroundColor;
                    Console.Write($"\n{time,-24:yyyy-MM-dd HH:mm:ss.fff} ");
                    Console.ForegroundColor = config.LogLevelToColorMap[logLevel];
                    Console.Write($"{StringifyLogLevel(logLevel),5} ");
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write($"{eventId.Id,-5} ");
                    Console.ForegroundColor = originalColor;
                    Console.Write(
                        $"--- [{Thread.CurrentThread.Name ?? Environment.CurrentManagedThreadId.ToString(),24}] " +
                        $"{WrapName(name, 50)}: {formatter(state, exception)}");
                    if (exception != null)
                        Console.Write("\n {0}", exception);
                }
            });
        else
            Task.Run(() =>
            {
                lock (Lock)
                {
                    Console.Write($"\n{time,-24:yyyy-MM-dd HH:mm:ss.fff} {StringifyLogLevel(logLevel),5} {eventId.Id,-5} " +
                                  $"--- [{Thread.CurrentThread.Name ?? Environment.CurrentManagedThreadId.ToString(),24}] " +
                                  $"{WrapName(name, 50)}: {formatter(state, exception)}");
                    if (exception != null)
                        Console.Write("\n {0}", exception);
                }
            });
    }
}

public static class SpringBootLoggerCloneExtensions
{
    public static ILoggingBuilder AddSpringBootLoggerClone(
        this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, SpringBootLoggerCloneProvider>());

        LoggerProviderOptions.RegisterProviderOptions
            <SpringBootLoggerCloneConfiguration, SpringBootLoggerCloneProvider>(builder.Services);

        return builder;
    }

    public static ILoggingBuilder AddSpringBootLoggerClone(
        this ILoggingBuilder builder,
        Action<SpringBootLoggerCloneConfiguration> configure)
    {
        builder.AddSpringBootLoggerClone();
        builder.Services.Configure(configure);

        return builder;
    }
}

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ColorConsole")]
public sealed class SpringBootLoggerCloneProvider : ILoggerProvider
{
    private readonly IDisposable? _onChangeToken;
    private SpringBootLoggerCloneConfiguration _currentConfig;
    private readonly ConcurrentDictionary<string, SpringBootLoggerClone> _loggers =
        new(StringComparer.OrdinalIgnoreCase);

    public SpringBootLoggerCloneProvider(
        IOptionsMonitor<SpringBootLoggerCloneConfiguration> config)
    {
        _currentConfig = config.CurrentValue;
        _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new SpringBootLoggerClone(name, GetCurrentConfig));

    private SpringBootLoggerCloneConfiguration GetCurrentConfig() => _currentConfig;

    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}
