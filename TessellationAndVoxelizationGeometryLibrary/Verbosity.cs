using System;
using System.Diagnostics;

namespace TVGL
{
    public static class Message
    {
        public static VerbosityLevels Verbosity = VerbosityLevels.OnlyCritical;

        /// <summary>
        ///  Calling Message.output will output the string, message, to the 
        ///  Console (a Debug message) but ONLY if the verbosity (see
        ///  below) is greater than or equal to your specified limit for this message.
        ///  the verbosity limit must be 0, 1, 2, 3, or 4.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="verbosityLimit">The verbosity limit.</param>
        internal static Boolean output(object message, int verbosityLimit = 0)
        {
            if ((verbosityLimit > (int)Verbosity)
                || (string.IsNullOrEmpty(message.ToString())))
                return false;
            Debug.WriteLine(message);
            return true;
        }
        /// <summary>
        /// Outputs the one item of the specified list corresponding to the particular verbosity.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns></returns>
        internal static Boolean output(params object[] list)
        {
            if (((int)Verbosity >= list.Length)
                || (string.IsNullOrEmpty(list[(int)Verbosity].ToString())))
                return false;
            Debug.WriteLine(list[(int)Verbosity]);
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
    };
}
