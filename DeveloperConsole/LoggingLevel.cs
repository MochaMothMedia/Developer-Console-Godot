using System;

namespace MochaMothMedia.DeveloperConsole
{
    [Flags]
    public enum LoggingLevel
    {
        None = 0,
        Message = 1 << 0,
        Warning = 1 << 1,
        Error = 1 << 2,
        Exception = 1 << 3,
        Assertion = 1 << 4
    }

    public static class LoggingLevelHelpers
    {
        public static LoggingLevel LoggingLevelAll => LoggingLevel.Message | LoggingLevel.Warning | LoggingLevel.Error | LoggingLevel.Exception | LoggingLevel.Assertion;
    }
}
