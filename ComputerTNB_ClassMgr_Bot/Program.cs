using MySql.Data.MySqlClient;
using System.Diagnostics.CodeAnalysis;

namespace ComputerTNB_ClassMgr_Bot
{
    internal class Program
    {
        #region Program_Variables

        /// <summary>
        /// DB Manager object for manipulating MySQL database.
        /// </summary>
        public static DBMgr db;

        /// <summary>
        /// Bot object for communicating with Telegram cloud.
        /// </summary>
        public static Bot bot;

        #endregion

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
            if (string.IsNullOrEmpty(mySql_DatabaseName))
                return 0;
            string? mySql_Username =
                Print_RequestPromptLine("Enter MySQL Username:\t");
            if (string.IsNullOrEmpty(mySql_Username))
                return 0;
            string? mySql_Password =
                Print_RequestPromptLine("Enter MySQL Password:\t");
            if (string.IsNullOrEmpty(mySql_Password))
                return 0;

            // Clear console screen.
            Console.Clear();

            // Create DBMgr object.
            try
            {
                db = new DBMgr(
                mySql_ServerName, mySql_DatabaseName, mySql_Username, mySql_Password
                );

                // Test connection.
                db.DBMS_TestConnection();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"ERROR Initializing MySQL DBMS Connection:\n{ex.Message}\n\nPress ENTER to exit...");
                Console.ReadLine();
                return -1;
            }

            // Get bot token.
            string? botToken =
                Print_RequestPromptLine("Enter BOT CLIENT SECRET TOKEN:\t");
            if (string.IsNullOrEmpty(botToken))
                return 0;

            // Establish a connection to global Telegram cloud.
            bot = new Bot(botToken);
            var me = bot.Bot_EstablishConnectionAsync().Result;
            if (me == null)
            {
                Console.ReadLine();
                return -2;
            }

            // Exit application successfully.
            return 0;
        }
    }
}