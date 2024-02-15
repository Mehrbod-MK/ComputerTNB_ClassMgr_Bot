using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<User?> Bot_EstablishConnectionAsync(CancellationToken ct = default)
        {
            try
            {
                // Establish a connection with cloud.
                botClient = new TelegramBotClient(this.botToken, this.customServer);

                // Get 'Me' object.
                var me = await botClient.GetMeAsync(ct);
                return me;
            }
            catch (Exception ex)
            {
                Logging.Log_Error(ex.Message, "Bot_EstablishConnectionAsync()");
                return null;
            }
        }

        #endregion
    }
}
