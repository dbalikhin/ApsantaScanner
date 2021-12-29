/*
 * From Security Code Scan
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */


#nullable disable
using ApsantaScanner.Security.Utils;
using System;

namespace ApsantaScanner.Security.Utils
{
    /// <summary>
    /// Logging utility to debug the analyzers
    /// </summary>
    internal class Logger
    {
        public static Action<string> LoggerHandler { get; set; }

        /// <summary>
        /// An action is set to handle the log to print to the console, redirect to the file system or anything else..
        /// </summary>
        /// <returns></returns>
        public static bool IsConfigured()
        {
            return LoggerHandler != null;
        }

        public static void Log(string message,
                               bool includeCallerInfo = true,
                               [System.Runtime.CompilerServices.CallerMemberName]
                               string memberName = "",
                               [System.Runtime.CompilerServices.CallerFilePath]
                               string sourceFilePath = "",
                               [System.Runtime.CompilerServices.CallerLineNumber]
                               int sourceLineNumber = 0)
        {
            if (!IsConfigured())
                return;

            if (includeCallerInfo) //Display the filename of the class calling the logging API
            {
                int indexBackSlash = sourceFilePath.LastIndexOf("\\", StringComparison.Ordinal);
                int indexForwardSlash = sourceFilePath.LastIndexOf("//", StringComparison.Ordinal);

                int lastSlash = Math.Max(Math.Max(indexBackSlash, indexForwardSlash) + 1, 0);

                LoggerHandler("[" + sourceFilePath.Substring(lastSlash) + ":" + sourceLineNumber + " " + memberName + "]");
            }

            LoggerHandler(message);
        }
    }
}
