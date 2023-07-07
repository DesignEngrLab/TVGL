// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Verbosity.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Diagnostics;

namespace TVGL
{
    /// <summary>
    /// Class Message.
    /// </summary>
    public static class Message
    {
        /// <summary>
        /// The verbosity sets how much is reported from the TVGL routines.
        /// </summary>
        public static VerbosityLevels Verbosity = VerbosityLevels.OnlyCritical;

        /// <summary>
        /// The text colors of the five levels of criticality
        /// </summary>
        static readonly ConsoleColor[] textColors = new[]
        {
            ConsoleColor.Red,
            ConsoleColor.DarkYellow,
            ConsoleColor.Cyan,
            ConsoleColor.Blue,
            ConsoleColor.Gray,
        };

        /// <summary>
        /// Calling Message.output will output the string, message, to the
        /// Console (a Debug message) but ONLY if the verbosity (see
        /// below) is greater than or equal to your specified limit for this message.
        /// the verbosity limit must be 0, 1, 2, 3, or 4.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="verbosityLimit">The verbosity limit.</param>
        /// <returns>Boolean.</returns>
        internal static bool output(object message, int verbosityLimit = 0)
        {
            if ((verbosityLimit > (int)Verbosity)
                || string.IsNullOrEmpty(message.ToString()))
                return false;
            Console.ForegroundColor = verbosityLimit >= textColors.Length ? textColors[^1] : textColors[verbosityLimit];
            var whiteSpace = new string(' ', verbosityLimit * 2);
            Console.WriteLine(whiteSpace + message);
            Console.ResetColor();
            return true;
        }
    }

    /// <summary>
    /// Setting the Verbosity to one of these values changes the amount of output
    /// send to the Debug Listener. Lower values may speed up search
    /// </summary>
    public enum VerbosityLevels
    {
        /// <summary>
        /// The only critical
        /// </summary>
        OnlyCritical = 0,

        /// <summary>
        /// The low
        /// </summary>
        Low = 1,

        /// <summary>
        /// The below normal
        /// </summary>
        BelowNormal = 2,

        /// <summary>
        /// The normal
        /// </summary>
        Normal = 3,

        /// <summary>
        /// The above normal
        /// </summary>
        AboveNormal = 4,

        /// <summary>
        /// The everything
        /// </summary>
        Everything = int.MaxValue
    }
}