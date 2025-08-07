using System;
using Microsoft.Extensions.Logging;

namespace TVGL
{
    internal static class Log
    {
        public static void BeginScope(string messageFormat, params object[] args)
          => Global.Logger.BeginScope(messageFormat, args);

        public static void Trace(string message, params object[] args)
            => Global.Logger.LogTrace(message, args);
        public static void Trace(Exception exception, string message, params object[] args)
            => Global.Logger.LogTrace(exception, message, args);

        public static void Debug(string message, params object[] args)
            => Global.Logger.LogDebug(message, args);
        public static void Debug(Exception exception, string message, params object[] args)
            => Global.Logger.LogDebug(exception, message, args);


        public static void Information(string message, params object[] args)
            => Global.Logger.LogInformation(message, args);
        public static void Information(Exception exception, string message, params object[] args)
            => Global.Logger.LogInformation(exception, message, args);


        public static void Warning(string message, params object[] args)
            => Global.Logger.LogWarning(message, args);
        public static void Warning(Exception exception, string message, params object[] args)
            => Global.Logger.LogWarning(exception, message, args);


        public static void Error(string message, params object[] args)
            => Global.Logger.LogError(message, args);
        public static void Error(Exception exception, string message, params object[] args)
            => Global.Logger.LogError(exception, message, args);

        public static void Critical(string message, params object[] args)
            => Global.Logger.LogCritical(message, args);
        public static void Critical(Exception exception, string message, params object[] args)
            => Global.Logger.LogCritical(exception, message, args);
    }
}
