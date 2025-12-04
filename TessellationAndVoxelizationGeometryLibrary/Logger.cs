using System;
using Microsoft.Extensions.Logging;

namespace TVGL
{
    internal static class Log
    {
        public static void BeginScope(string messageFormat, params object[] args)
          => OutputServices.Logger.BeginScope(messageFormat, args);

        public static void Trace(string message, params object[] args)
            => OutputServices.Logger.LogTrace(message, args);
        public static void Trace(Exception exception, string message, params object[] args)
            => OutputServices.Logger.LogTrace(exception, message, args);

        public static void Debug(string message, params object[] args)
            => OutputServices.Logger.LogDebug(message, args);
        public static void Debug(Exception exception, string message, params object[] args)
            => OutputServices.Logger.LogDebug(exception, message, args);


        public static void Information(string message, params object[] args)
            => OutputServices.Logger.LogInformation(message, args);
        public static void Information(Exception exception, string message, params object[] args)
            => OutputServices.Logger.LogInformation(exception, message, args);


        public static void Warning(string message, params object[] args)
            => OutputServices.Logger.LogWarning(message, args);
        public static void Warning(Exception exception, string message, params object[] args)
            => OutputServices.Logger.LogWarning(exception, message, args);


        public static void Error(string message, params object[] args)
            => OutputServices.Logger.LogError(message, args);
        public static void Error(Exception exception, string message, params object[] args)
            => OutputServices.Logger.LogError(exception, message, args);

        public static void Critical(string message, params object[] args)
            => OutputServices.Logger.LogCritical(message, args);
        public static void Critical(Exception exception, string message, params object[] args)
            => OutputServices.Logger.LogCritical(exception, message, args);
    }
}
