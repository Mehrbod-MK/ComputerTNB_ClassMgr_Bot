using System.Runtime.CompilerServices;

namespace ComputerTNB_ClassMgr_Bot
{
    internal class Program
    {
        private static void PrintWelcomeMessage()
        {
            Console.WriteLine(
                "In the name of God.\n\n" +
                "IAU-TNB Computer Group - Class Manager Backend Software\n" +
                "Version  0.0.2.0\n\n" +
                "Created by:  Mehrbod Molla Kazemi\n" +
                "Professor:  Dr. Rahman AlimohammadZadeh\n" +
                "Date:  1402-11-26 (February 2024)\n\n"
                );
        }

        public static string? Print_RequestPromptLine(string? message)
        {
            Console.Write(message);
            return Console.ReadLine();
        }

        static int Main(string[] args)
        {
            // Print welcome messages.
            PrintWelcomeMessage();

            // Request MySQL Client information.
            string? mySql_ServerName = 
                Print_RequestPromptLine("Enter MySQL Client Server name (default: localhost):\t");
            if (string.IsNullOrEmpty(mySql_ServerName))
                mySql_ServerName = @"localhost";
            string? mySql_DatabaseName =
                Print_RequestPromptLine("Enter MySQL Database name:\t");
            if (string.IsNullOrEmpty(mySql_ServerName))
                return 0;
            string? mySql_Username =
                Print_RequestPromptLine("Enter MySQL Username:\t");
            if (string.IsNullOrEmpty(mySql_Username))
                return 0;
            string? mySql_Password =
                Print_RequestPromptLine("Enter MySQL Password:\t");
            if (string.IsNullOrEmpty(mySql_Password))
                return 0;

            // Exit application successfully.
            return 0;
        }
    }
}