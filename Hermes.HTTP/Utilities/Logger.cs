using Hermes.HTTP.Enums;

namespace Hermes.HTTP.Utilities;

internal class Logger
{
    private static LogLevel _minLevel = LogLevel.Debug;
    
    public static void SetLogLevel(LogLevel level)
    {
        _minLevel = level;
    }

    public static void Debug(string message)
    {
        Log(LogLevel.Debug, message);
    }
    
    public static void Info(string message)
    {
        Log(LogLevel.Info, message);
    }
    
    public static void Warning(string message)
    {
        Log(LogLevel.Warning, message);
    }
    
    public static void Error(string message)
    {
        Log(LogLevel.Error, message);
    }

    private static void Log(LogLevel level, string message)
    {
        if (level < _minLevel)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] [{level}] {message}";
        
        ConsoleColor originalColor = Console.ForegroundColor;
        
        switch (level)
        {
            case LogLevel.Debug:
                Console.ForegroundColor = ConsoleColor.Cyan;
                break;
            case LogLevel.Info:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case LogLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogLevel.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogLevel.Fatal:
                Console.ForegroundColor = ConsoleColor.DarkRed;
                break;
        }
        
        Console.WriteLine(logMessage);
        Console.ForegroundColor = originalColor;
    }
}