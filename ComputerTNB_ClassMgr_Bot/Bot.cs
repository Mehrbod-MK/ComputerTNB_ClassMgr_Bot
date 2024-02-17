using ComputerTNB_ClassMgr_Bot.Models;
using ComputerTNB_ClassMgr_Bot.Resources.Strings;
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
        #region Bot_Constants

        const string SYSTEM_ADMIN_ID = @"@MAD_Gametronics";

        #endregion

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
            if (botClient == null)
                throw new NullReferenceException();

            try
            {
                long chatID = message.Chat.Id;

                // Check role of user.
                var role = await Program.db.SQL_GetUserRole(chatID);

                // Process message based on user's role.
                switch (role)
                {
                    // User's role is unknown, probably new...
                    case DBMgr.User_Roles.Unknown:
                        await Process_Message_Unknown_User_Async(message);
                        break;

                    // User is a teacher.
                    case DBMgr.User_Roles.Teacher:
                        await Process_Message_Teacher_User_Async(message);
                        break;
                }
            }
            catch(Exception ex)
            {
                try
                {
                    // Log error.
                    Logging.Log_Error(ex.Message, "Process_Message_Async()");

                    await botClient.SendTextMessageAsync(
                        message.Chat.Id,
                        $"🚫 متأسفانه، خطای زیر در هنگام پردازش پیام ورودی به وقوع پیوست:\n\n❌ <b>{ex.Message}</b>\n\n👈 <i>لطفاً لحظاتی بعد تلاش نمایید یا اگر مشکل رفع نشد، با راهبر سیستم تماس حاصل فرمایید.</i>",
                        null,
                        Telegram.Bot.Types.Enums.ParseMode.Html,
                        null,
                        null,
                        false, true, message.MessageId, true, null
                        );
                }
                catch(Exception) { }
            }
        }

        /// <summary>
        /// Processes an incoming Telegram Message JSON object for an unkown user.
        /// </summary>
        /// <param name="message">JSON message structure.</param>
        /// <returns>This task returns nothing.</returns>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="Telegram.Bot.Exceptions.ApiResponse"></exception>
        public async Task Process_Message_Unknown_User_Async(Message message)
        {
            // Check BOT client.
            if(botClient == null)
            {
                throw new NullReferenceException();
            }

            // Send registration message.
            await botClient.SendTextMessageAsync(
                message.Chat.Id,

                "👋 سلام و درود ویژه خدمت شما کاربر گرامی بزرگوار\n" +
                "⭐ به سامانه مدیریت امور کلاسی گروه کامپیوتر تهران شمال خوش آمدید.\n\n" +
                $"⚠ <strong>شماره نشست کاربری فعال شما:</strong> <code>{message.Chat.Id}</code>\n\n" + 
                $"👈 این پیام را برای راهبر سیستم به آیدی <b>{SYSTEM_ADMIN_ID}</b> فوروارد (هدایت) کرده و سپس مشخصات ذیل را برای ایشان ارسال کنید تا ثبت نام شما در سیستم صورت گیرد:\n\n" +
                "1️⃣ سِمَت شما (دانشجو، استاد، ادمین)\n2️⃣نام و نام خانوادگی کامل\n3️⃣ شماره تماس منطبق با این نشست کاربری\n4️⃣ پست الکترونیکی (ایمیل)\n\n🙏 <i>سپاس از توجه شما.</i>",

                null,
                Telegram.Bot.Types.Enums.ParseMode.Html,
                null,
                null,
                false, false, message.MessageId, true, null
                );

            // Log.
            Logging.Log_Information($"Welcomed new user, {message.Chat.Id}", message.Chat.Id.ToString());
        }

        /// <summary>
        /// Processes an incoming Telegram Message JSON object for an unkown user.
        /// </summary>
        /// <param name="message">JSON message structure.</param>
        /// <returns>This task returns nothing.</returns>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="Telegram.Bot.Exceptions.ApiResponse"></exception>
        /// <exception cref="Exception">Other exceptions...</exception>
        public async Task Process_Message_Teacher_User_Async(Message message)
        {
            // Check BOT client.
            if (botClient == null)
                throw new NullReferenceException();

            var chatID = message.Chat.Id;
            var teacherQuery = await Program.db.SQL_GetTeacher(chatID);

            // Check for errors.
            if (teacherQuery.success == false && teacherQuery.exception != null)
                throw teacherQuery.exception;

            // Check if query is NULL!
            if (teacherQuery.result == null)
                throw new InvalidDataException();

            // Obtained teacher.
            var teacher = (Teacher)teacherQuery.result;

            // UPDATE DATABASE VALUES.

            ///////////////////////////// PROCESS TEXT MESSAGE /////////////////////////////
            if (message.Text != null)
            {

                switch(teacher.state)
                {

                }

            }
            ////////////////////////////////////////////////////////////////////////////////
        }

        #endregion
    }
}
