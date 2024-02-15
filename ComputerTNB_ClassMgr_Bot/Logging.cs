using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerTNB_ClassMgr_Bot
{
    /// <summary>
    /// Static class containing methods for logging to console.
    /// </summary>
    public static class Logging
    {
        public static void Log_Error(string msg, 
            string? callingMethod = null,
            bool carriageReturn = true, bool includDate = true)
        {
            if (includDate)
                Console.Write($"[{DateTime.Now}]\t");

            if (!string.IsNullOrEmpty(callingMethod))
                Console.Write($"ERROR at {callingMethod}:\t");
            else
                Console.Write("ERROR:\t");

            if (carriageReturn)
                Console.WriteLine(msg);
            else
                Console.Write(msg);                
        }

        public static void Log_Information(string msg,
            string? callingMethod = null,
            bool carriageReturn = true, bool includDate = true)
        {
            if (includDate)
                Console.Write($"[{DateTime.Now}]\t");

            if (!string.IsNullOrEmpty(callingMethod))
                Console.Write($"INFO at {callingMethod}:\t");
            else
                Console.Write("INFO:\t");

            if (carriageReturn)
                Console.WriteLine(msg);
            else
                Console.Write(msg);
        }
    }
}
