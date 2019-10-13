﻿using System;
using System.IO;

//this one comes from https://github.com/crosire/scripthookvdotnet/wiki/Code-Snippets#logger-helper
//Thanks a lot!
namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// Static logger class that allows direct logging of anything to a text file
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// logs the message to a file... 
        /// but only if the log level defined in the mod options is greater or equal to the message's level
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logLevel"></param>
        public static void Log(object message, int logLevel)
        {
            if (ModOptions.Instance == null) return;
            if (ModOptions.Instance.LoggerLevel >= logLevel)
            {
                File.AppendAllText("GangAndTurfMod-" + DateTime.Today.ToString("yyyy-MM-dd") + ".log", DateTime.Now + " : " + message + Environment.NewLine);
            }

        }
    }
}
