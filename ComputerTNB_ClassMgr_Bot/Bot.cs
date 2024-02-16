using ComputerTNB_ClassMgr_Bot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace ComputerTNB_ClassMgr_Bot
{
    /// <summary>
    /// Class used for handling Bot operations for application.
    /// </summary>
    public class Bot
    {
        #region Bot_Variables

        private string botToken = "";
        private HttpClient? customServer = null;

        private TelegramBotClient? botClient = null;

        #endregion

        #region Bot_Methods

        /// <summary>
        /// Default Bot Ctor.
        /// </summary>
        public Bot(string botGlobalToken, bool autoConnect = false, HttpClient? customServer = null)
        {
            this.botToken = botGlobalToken;
            this.customServer = customServer;

            if(autoConnect)
                botClient = new TelegramBotClient(botGlobalToken, customServer);
        }

        /// <summary>
        /// Establishes a connection to telegram cloud.
        /// </summary>
        /// <param name="ct">Task cancellation token signal.</param>
        /// <returns>This task returns a Telegram User object.</returns>
        public async Task<User?> Bot_EstablishConnectionAsync(CancellationToken ct = default)
        {
            try
            {
                Logging.Log_Information("Establishing a connection with cloud...");

                // Establish a connection with cloud.
                botClient = new TelegramBotClient(this.botToken, this.customServer);

                // Get 'Me' object.
                var me = await botClient.GetMeAsync(ct);

                Logging.Log_Information($"Connection established at:\thttps://api.telegram.org/{me.Username}.");

                return me;
            }
            catch (Exception ex)
            {
                Logging.Log_Error(ex.Message, "Bot_EstablishConnectionAsync()");
                return null;
            }
        }

        /// <summary>
        /// Main task for polling updates from cloud.
        /// </summary>
        /// <returns>This task is in loop and returns nothing.</returns>
        public async Task Bot_PollLoopAsync(CancellationTokenSource? ct = null)
        {
            int updateOffset = 0;

            if (botClient == null)
                return;

            Logging.Log_Warning_OnCondition(Program.db == null,
                "Database is not initialized."
                );
            if (Program.db == null)
                return;

            while(true)
            {
                // Check cancellation event.
                if (ct != null)
                    if (ct.IsCancellationRequested)
                        break;

                try
                {
                    var updates = await botClient.GetUpdatesAsync(updateOffset, null, 3);

                    // Check incoming updates.
                    foreach(var update in updates)
                    {
                        updateOffset = update.Id + 1;

                        // Check incoming update type.
                        switch(update.Type)
                        {
                            // Update is unknown!
                            case Telegram.Bot.Types.Enums.UpdateType.Unknown:
                                Logging.Log_Warning($"Unkown update received with ID:\t{update.Id}.", 
                                    "Bot_PollLoopAsync()");
                                break;

                            case Telegram.Bot.Types.Enums.UpdateType.Message:
                                if (update.Message != null)
                                    await Process_Message_Async(update.Message);
                                break;
                        }
                    }
                }
                catch(Exception ex)
                {
                    
                }
            }
        }

        public async Task Process_Message_Async(Message message)
        {
            var callDB = await Program.db.SQL_GetStudent(message.Chat.Id);

            
        }

        #endregion
    }
}
