using ComputerTNB_ClassMgr_Bot.Models;
using ComputerTNB_ClassMgr_Bot.Resources.Strings;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

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

                            // Update is a message object.
                            case Telegram.Bot.Types.Enums.UpdateType.Message:
                                if (update.Message != null)
                                    await Process_Message_Async(update.Message);
                                break;

                            // Update is a CallbackQuery.
                            case Telegram.Bot.Types.Enums.UpdateType.CallbackQuery:
                                if (update.CallbackQuery != null)
                                {
                                    await Process_CallbackQuery_Async(update.CallbackQuery);
                                }
                                break;
                        }
                    }
                }
                catch(Exception ex)
                {
                    
                }
            }
        }

        public async Task Process_CallbackQuery_Async(CallbackQuery cbQuery)
        {
            if (botClient == null)
                throw new NullReferenceException();

            // Process callback queries containing message.
            if(cbQuery.Message != null)
            {
                long chatID = cbQuery.Message.Chat.Id;

                // Process CBQUERY_MACRO Messages.
                if (cbQuery.Data == "CLOSE_LESSON_PANEL")
                {
                    await botClient.AnswerCallbackQueryAsync(cbQuery.Id,
                        "پنجره بسته شد ✅", false);

                    Logging.Log_Information(
                        "Closed LESSONS panel.",
                        $"Process_CallbackQuery_Async({chatID})"
                        );

                    await botClient.DeleteMessageAsync(
                        chatID,
                        cbQuery.Message.MessageId
                        );
                }

                // Process callback query based on user's role.
                var userRole = await Program.db.SQL_GetUserRole(chatID);

                if (userRole == DBMgr.User_Roles.Teacher)
                {
                    var teacherQuery = await Program.db.SQL_GetTeacher(chatID);
                    if (teacherQuery.exception != null)
                        throw teacherQuery.exception;
                    if (teacherQuery.result == null)
                        throw new NullReferenceException();

                    var teacher = (Teacher)teacherQuery.result;

                    await Process_CallbackQuery_Teacher_Async(teacher, cbQuery);
                }
                else
                {
                    
                }
            }
        }

        private async Task Process_CallbackQuery_Teacher_Async(Teacher teacher, CallbackQuery cbQuery)
        {
            if (botClient == null)
                throw new NullReferenceException();

            if (cbQuery.Data != null)
            {
                var datas = cbQuery.Data.Split('~');

                // Get the list of students (LIST_STUD~{PresentationCode})
                if (datas[0] == "LIST_STUDS")
                {
                    try
                    {
                        var listStudentsQuery =
                            await Program.db.SQL_GetListOfLessonStudents(datas[1]);

                        if (listStudentsQuery.exception != null)
                            throw listStudentsQuery.exception;
                        if (listStudentsQuery.result == null)
                            throw new NullReferenceException();

                        var students = (List<Student>)listStudentsQuery.result;
                        if (students.Count <= 0)
                        {
                            Logging.Log_Information(
                                $"No students to show for presentation code {datas[1]}.",
                                $"Process_CallbackQuery_Teacher_Async({teacher.chatID}, {cbQuery.Id}) -> LIST_STUDS"
                                );

                            await botClient.AnswerCallbackQueryAsync(cbQuery.Id,
                                "🔍 جستجو نتیجه‌ای در بر نداشت. ❕", true);
                        }
                        else
                        {

                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }

                // Check the attendence of students by image processing.
                else if (datas[0] == "ATTENDENCE_AI")
                {
                    try
                    {
                        var lesson = (Lesson?)(await Program.db.SQL_GetLesson(datas[1])).result;
                        if (lesson == null)
                            throw new NullReferenceException();

                        // Check the time of lesson. If outside of range, tell the teacher that they cannot check it.
                        if(!Program._IGNORE_TEACHER_LESSON_CHECKCLASSTIME && !DBMgr.Check_TimeIsCurrentForAttendence(lesson.lessonDateTime, lesson.lessonEndTime))
                        {
                            Logging.Log_Warning($"Time has not reached for lesson {lesson.presentationCode}!",
                                $"Process_CallbackQuery_Teacher_Async({teacher.chatID}, {cbQuery.Id}) -> ATTENDENCE_AI");

                            await botClient.AnswerCallbackQueryAsync(
                                cbQuery.Id,
                                $"⚠ استاد {teacher.fullName} ({teacher.chatID}) در این زمان، مجاز به حضور و غیاب کلاس درس {lesson.lessonName} ({lesson.presentationCode}) نمی‌باشد.",
                                true
                                );
                        }
                        else
                        {
                            // Answer callback query.
                            await botClient.AnswerCallbackQueryAsync(cbQuery.Id);

                            // Update teacher's state and set metadata to PRESENTATION-CODE of the lesson to check its attendence status.
                            await Program.db.SQL_Set_User_State(teacher, (uint)DBMgr.User_States.Teacher_Checking_Lesson_Attendence);
                            await Program.db.SQL_Set_User_MetaData(teacher, datas[1]);

                            // Prompt user to send images for processing.
                            List<List<KeyboardButton>> keyboardButtons_BeginImgProc = new List<List<KeyboardButton>>()
                            {
                                new List<KeyboardButton>()
                                {
                                    KeyboardButton.WithRequestUser("👨‍🎓 انتخاب چت/نشست کاربری دانشجو به صورت دستی 👩‍🎓", new KeyboardButtonRequestUser() { UserIsBot = false, }),
                                },
                                new List<KeyboardButton>()
                                {
                                    new KeyboardButton("🏁 پایان عملیات حضور و غیاب 🏁"),
                                },
                            };
                            await botClient.SendTextMessageAsync(
                                teacher.chatID,
                                $"👈 پنل حضور و غیاب\n\n" +
                                $"👨‍🏫👩‍🏫 استاد: <b> {teacher.fullName} ({teacher.chatID})</b>\n" +
                                $"🏛 کلاس:  <b><u>{lesson.lessonName} ({lesson.presentationCode})\n\n</u></b>" +
                                $"📷 سامانه آماده پردازش تصویر چهره دانشجویان حاضر در کلاس می باشد.\n" +
                                $"🖼 به یاد داشته باشید که تا حد امکان، تصویر واضج باشد. سامانه، اطراف چهره هایی که شناسایی می کند، مستطیلی می کشد. سپس شما با تأیید هویت تشخیص داده شده، حضور و غیاب دانشجوی محترم را تأیید میفرمایید.\n\n" +
                                $"<i>برای باز کردن دوربین، بر روی دکمه سنجاق 🧷 در پیام رسان کلیک کنید: 👇</i>",
                                null, Telegram.Bot.Types.Enums.ParseMode.Html, null,
                                false, false, true, null, true, new ReplyKeyboardMarkup(keyboardButtons_BeginImgProc)
                                );
                        }
                    }
                    catch(Exception ex)
                    {
                        
                    }
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
            var updateLastActivity_Query = await Program.db.SQL_ExecuteWrite(
                "UPDATE teachers " +
                $"SET LastActivity = \'{DBMgr.Convert_FromDateTime_ToSQLString(DateTime.Now)}\' " +
                $"WHERE ChatID = \'{chatID}\'"
                );

            ///////////////////////////// PROCESS TEXT MESSAGE /////////////////////////////
            if (message.Text != null)
            {
                string msg_Text = message.Text;
                string msg_Text_Raw = msg_Text.Trim().ToLower();

                switch(teacher.state)
                {
                    case (uint)DBMgr.User_States.At_MainMenu:
                        
                        if(msg_Text_Raw == "/start")
                        {
                            await Prompt_Menu_Main_Teacher(teacher);
                        }
                        else if(msg_Text_Raw == "/cancel")
                        {
                            await Prompt_NoOperationsToCancel(teacher.chatID, message);
                        }
                        else if(msg_Text_Raw == "/back")
                        {
                            await Prompt_NothingToGetBackTo(teacher.chatID, message);
                        }

                        else
                        {
                            // Prompt list of classes.
                            if(msg_Text == "🏛 مدیریت کلاس‌های من")
                            {
                                await Prompt_Teacher_ListOfLessons
                                    (teacher, message);
                            }

                            
                        }

                        break;

                    case (uint)DBMgr.User_States.Teacher_Viewing_Lessons:

                        if(msg_Text_Raw == "/start")
                        {
                            await Prompt_Teacher_ListOfLessons(teacher, message);
                        }
                        else if(msg_Text_Raw == "/back" || msg_Text_Raw == "/cancel")
                        {
                            await Prompt_Menu_Main_Teacher(teacher);
                        }

                        else
                        {
                            
                            // Get back to previous menu.
                            if(msg_Text == "🔙 بازگشت به منوی قبلی 🔙")
                            {
                                await Prompt_Menu_Main_Teacher(teacher);
                            }

                            // Process lessson name.
                            else
                            {
                                var lessonTrims = msg_Text.Split("👈");

                                string presentationCode = lessonTrims[0].Trim();

                                await Prompt_Teacher_Lesson_Panel(presentationCode, teacher, message);
                            }
                        }

                        break;
                }

            }
            ////////////////////////////////////////////////////////////////////////////////

            ///////////////////////////// PROCESS PHOTO MESSAGE /////////////////////////////
            else if(message.Photo != null)
            {
                // Obtain the best quality photo.
                PhotoSize bestQualityPhoto = message.Photo[0];
                foreach(var photoQual in message.Photo)
                {
                    if(photoQual.FileSize > bestQualityPhoto.FileSize)
                    {
                        bestQualityPhoto = photoQual;
                    }
                }

                // Check teacher state.
                switch(teacher.state)
                {
                    case (uint)DBMgr.User_States.Teacher_Checking_Lesson_Attendence:
                        // Send "Loading..." emoji.
                        var msg = await botClient.SendTextMessageAsync(teacher.chatID, "⌛");

                        // Download the photo of the students in the class.
                        var download_Query = await Program.db.FILE_Photo_SubmitToDirectory(botClient, bestQualityPhoto);
                        if (download_Query.exception != null)
                            throw download_Query.exception;
                        if (download_Query.result == null)
                            throw new NullReferenceException();

                        // Send this to AI model for face recognition.
                        var photoFilePath = (string)download_Query.result;
                        var photoFileExtension = Path.GetExtension(photoFilePath);
                        var faces = Program.ai.AI_DetectAndTagFaces(photoFilePath, out OpenCvSharp.Mat rendered);

                        // Save rendered picture to local TEMP directory.
                        var finalPath = AI_ImgProc.Mat_Save_Temp(rendered, photoFileExtension);

                        // Upload render result after temp save.
                        using(FileStream fs = new FileStream(finalPath, FileMode.Open))
                        {
                            var photo = InputFile.FromStream(fs, Path.GetFileName(photoFilePath));
                            await botClient.SendPhotoAsync(teacher.chatID, photo);
                        }

                        // Delete Loading msg.
                        await botClient.DeleteMessageAsync(teacher.chatID, msg.MessageId);

                        break;
                }
            }

            /////////////////////////////////////////////////////////////////////////////////
        }

        private async Task Prompt_Menu_Main_Teacher(Teacher teacher)
        {
            if (botClient == null)
                throw new NullReferenceException();

            List<List<KeyboardButton>> keyboard_MainMenu_Teacher = new List<List<KeyboardButton>>()
            {
                new List<KeyboardButton>()
                {
                    new KeyboardButton("🏛 مدیریت کلاس‌های من"),
                },

                new List<KeyboardButton>()
                {
                    new KeyboardButton("🤚 حضور و غیاب"),
                },

                new List<KeyboardButton>()
                {
                    new KeyboardButton("📛 بررسی اعتراض به نمره"),
                    new KeyboardButton("🥇 ثبت نمره"),
                },
            };

            string promptText = $"👋 سلام و درود بر شما، {teacher.fullName} بزرگوار،\n" +
                $"⭐ به سامانه تلگرامی مدیریت امور کلاسی گروه کامپیوتر تهران شمال خوش آمدید" +
                $"\n\n👈 شماره نشست کاربری شما:  <code>{teacher.chatID}</code>\n" +
                $"\n📅 تاریخ تأیید عضویت:  {DBMgr.Convert_FromDateTime_ToPersianLongDateTimeString(teacher.joinedDate)}\n" +
                $"📅 آخرین زمان تعامل با سامانه:  {DBMgr.Convert_FromDateTime_ToPersianLongDateTimeString(teacher.lastActivity)}\n\n" +
                $"❔ چه کمکی از ما ساخته است؟ از منوی ذیل، انتخاب کنید: 👇";

            // Update teacher's state.
            await Program.db.SQL_Set_User_State(teacher, (uint)DBMgr.User_States.At_MainMenu);

            await botClient.SendTextMessageAsync(
                teacher.chatID,
                promptText,
                null,
                Telegram.Bot.Types.Enums.ParseMode.Html,
                null, null, true, true, null, true,
                new ReplyKeyboardMarkup(keyboard_MainMenu_Teacher)
                );

            Logging.Log_Information("Displayed main menu.", 
                $"SHOW_WELCOME_TEACHER_MSG({teacher.chatID})");
        }

        private async Task Prompt_NoOperationsToCancel(long chatID, Message replyTo)
        {
            if (botClient == null)
                throw new NullReferenceException();

            await botClient.SendTextMessageAsync(chatID,
                "❗ عملیاتی برای لغو کردن وجود ندارد.",
                null, Telegram.Bot.Types.Enums.ParseMode.Html, null, null,
                true, null, replyTo.MessageId, true);
        }

        private async Task Prompt_NothingToGetBackTo(long chatID, Message replyTo)
        {
            if (botClient == null)
                throw new NullReferenceException();

            await botClient.SendTextMessageAsync(chatID,
                "❗ منویی برای بازگشت به آن وجود ندارد.",
                null, Telegram.Bot.Types.Enums.ParseMode.Html, null, null,
                true, null, replyTo.MessageId, true);
        }

        private async Task Prompt_Teacher_ListOfLessons(Teacher teacher, Message replyTo)
        {
            if (botClient == null)
                throw new NullReferenceException();

            var db_GetTeacherLessons = 
                await Program.db.SQL_GetListOfTeacherLessons(teacher.chatID);

            if(db_GetTeacherLessons.success != false && db_GetTeacherLessons.result == null)
            {
                if (db_GetTeacherLessons.exception != null)
                    await Bot_SendTextMessage_Error_Async(
                        teacher.chatID, $"Cannot fetch lessons of the teacher {teacher.chatID}",
                        db_GetTeacherLessons.exception.Message, "بارگذاری لیست دروس استاد",
                        $"Prompt_Teacher_Lessons({teacher.chatID})"
                        );
                else
                    await Bot_SendTextMessage_Error_Async(
                        teacher.chatID, $"Cannot fetch lessons of the teacher {teacher.chatID}",
                        "خطای نامعلوم.", "بارگذاری لیست دروس استاد",
                        $"Prompt_Teacher_Lessons({teacher.chatID})"
                        );

                return;
            }

            // Check for null list.
            if (db_GetTeacherLessons.result == null)
                throw new NullReferenceException();

            // Gather list of lessons.
            var listOfLessons = (List<Lesson>)db_GetTeacherLessons.result;

            // Prompt list of lessons if available.
            if(listOfLessons.Count <= 0)
            {
                await Bot_SendTextMessage_Information_Async(teacher.chatID,
                    $"No lessons to show for Teacher.", "موردی برای نمایش وجود ندارد.",
                    $"Prompt_Teacher_Lessons({teacher.chatID})");

                return;
            }

            // List lessons.
            List<List<KeyboardButton>> keyboardButtons_Lessons = new List<List<KeyboardButton>>();
            foreach(var lesson in listOfLessons)
            {
                keyboardButtons_Lessons.Add(new List<KeyboardButton>()
                {
                    new KeyboardButton($"{lesson.presentationCode} 👈 {lesson.lessonName} 👈 {lesson.lessonCode}"),
                });
            }
            // Add one extra option for getting back to previous menu.
            keyboardButtons_Lessons.Add(new List<KeyboardButton>()
            {
                new KeyboardButton("🔙 بازگشت به منوی قبلی 🔙"),
            });

            // Change state of teacher.
            var db_SetStateResult = await Program.db.SQL_Set_User_State(teacher, (uint)DBMgr.User_States.Teacher_Viewing_Lessons);
            if (db_SetStateResult.exception != null)
                throw db_SetStateResult.exception;

            // Display in a text message.
            await botClient.SendTextMessageAsync(
                teacher.chatID,
                $"👋 استاد گرامی، شما تعداد {listOfLessons.Count} درس تعریف شده دارید.\n\n👇 برای مشاهده جزئیات هر کلاس، بر روی یکی از کلیدهای ذیل کلیک کنید تا پنل کلاس برای شما نمایش داده شود:",
                null, Telegram.Bot.Types.Enums.ParseMode.Html,
                null, null, false, true, replyTo.MessageId, true, 
                new ReplyKeyboardMarkup(keyboardButtons_Lessons)
                );

            // Log.
            Logging.Log_Information("Displayed list of lessons for Teacher.", $"Prompt_Teacher_Lessons({teacher.chatID})");

            
        }

        private async Task Bot_SendTextMessage_Error_Async(long chatID,
            string errorLog,
            string msgText, string command, string? callingMethod = null)
        {
            try
            {
                Logging.Log_Error(errorLog, callingMethod);

                string prompt = $"🚫 خطای ذیل در اجرای فرمان <u>{command}</u> به وقوع پیوست:\n\n" +
                    $"❌ <b>{msgText}</b>\n\n👈 <i>برای بازتولید پیام قبلی، از /start استفاده کنید.</i>";

                if (botClient != null)
                    await botClient.SendTextMessageAsync(chatID, prompt, null,
                        Telegram.Bot.Types.Enums.ParseMode.Html, null, null,
                        false, false, null, true,
                        new ReplyKeyboardRemove());
            }
            catch(Exception ex)
            {
                Logging.Log_Error(ex.Message, "Bot_SendTextMessage_Error_Async(...)");
            }
        }

        private async Task Bot_SendTextMessage_Information_Async(long chatID,
            string infoLog,
            string msgText, string? callingMethod = null)
        {
            try
            {
                Logging.Log_Information(infoLog, callingMethod);

                string prompt = $"❕ <b>{msgText}</b>\n\n👈 <i>برای بازتولید پیام قبل، از /start استفاده کنید.</i>";

                if (botClient != null)
                    await botClient.SendTextMessageAsync(chatID, prompt, null,
                        Telegram.Bot.Types.Enums.ParseMode.Html, null, null,
                        false, false, null, true, 
                        new ReplyKeyboardRemove());
            }
            catch (Exception ex)
            {
                Logging.Log_Error(ex.Message, "Bot_SendTextMessage_Info_Async(...)");
            }
        }

        private async Task Bot_SendTextMessage_Warning_Async(long chatID,
            string infoLog,
            string msgText, string? callingMethod = null)
        {
            try
            {
                Logging.Log_Warning(infoLog, callingMethod);

                string prompt = $"⚠ <b>{msgText}</b>\n\n👈 <i>برای بازتولید پیام قبل، از /start استفاده کنید.</i>";

                if (botClient != null)
                    await botClient.SendTextMessageAsync(chatID, prompt, null,
                        Telegram.Bot.Types.Enums.ParseMode.Html, null, null,
                        false, false, null, true,
                        new ReplyKeyboardRemove());
            }
            catch (Exception ex)
            {
                Logging.Log_Error(ex.Message, "Bot_SendTextMessage_Warning_Async(...)");
            }
        }

        private async Task Prompt_Teacher_Lesson_Panel(string lesson_PresentationCode, Teacher teacher, Message message)
        {
            if (botClient == null)
                throw new NullReferenceException();

            var db_GetLessonQuery = 
                await Program.db.SQL_GetLesson(lesson_PresentationCode);

            // Check if a DB exception occured.
            if (db_GetLessonQuery.exception != null)
                throw db_GetLessonQuery.exception;

            // Check if there are no lessons to show.
            if(db_GetLessonQuery.result == null)
            {
                await Bot_SendTextMessage_Warning_Async(
                    teacher.chatID,
                    "No such lesson available!",
                    $"چنین کلاس درسی با کد ارائه {lesson_PresentationCode} وجود ندارد.",
                    $"Prompt_Teacher_Lesson_Panel({teacher.chatID})"
                    );

                return;
            }

            var lesson = (Lesson)db_GetLessonQuery.result;

            // Check if teacher doesn't have the right to view lesson.
            if(lesson.teacherChatID != teacher.chatID)
            {
                await Bot_SendTextMessage_Error_Async(
                    teacher.chatID,
                    $"Access denied - Teacher cannot view the lesson of another teacher with ChatID {lesson.teacherChatID}",
                    $"محدودیت دسترسی وجود دارد - شما نمی‌توانید پنل درس برای استاد دیگر را مشاهده فرمایید!",
                    $"Prompt_Teacher_Lesson_Panel({teacher.chatID})"
                    );
            }

            // Display lesson control panel.
            string prompt =
                $"👈 پنل درس\n\n🔢 کد درس:    <code>{lesson.lessonCode}</code>\n" +
                $"🔢 کد ارائه:    <code>{lesson.presentationCode}</code>\n\n" +
                $"📖 نام درس:    <b>{lesson.lessonName}</b>\n" +
                $"👨‍🏫 نام استاد:  <b>{teacher.fullName}</b>\n\n" +
                $"⌚ زمان کلاس:  <b>{DBMgr.Convert_FromDateTime_ToLessonDateTimeLongString(lesson)}</b>\n" +
                $"📅 تاریخ آزمون:   <b>{DBMgr.Convert_FromDateTime_ToPersianLongDateTimeString(lesson.examDateTime)}</b>\n\n" +
                $"🏛 مکان برگزاری کلاس درس:    <b>{lesson.className}</b>";

            // Inline buttons.
            List<List<InlineKeyboardButton>> inlineButtons_Teacher_Lesson = new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("👨‍🎓 لیست دانشجویان", $"LIST_STUDS~{lesson.presentationCode}"),
                },

                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("🎓 حضور و غیاب دانشجویان با پردازش تصویر 📸", $"ATTENDENCE_AI~{lesson.presentationCode}"),
                },

                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("❌ بستن پنجره درس", "CLOSE_LESSON_PANEL"),
                }
            };

            // Generate prompt.
            await botClient.SendTextMessageAsync(
                teacher.chatID,
                prompt, null, Telegram.Bot.Types.Enums.ParseMode.Html,
                null, null, false, true, message.MessageId, false,
                new InlineKeyboardMarkup(inlineButtons_Teacher_Lesson)
                );

            // Log.
            Logging.Log_Information(
                "Displayed LESSON panel to teacher.",
                $"Prompt_Teacher_Lesson_Panel({lesson_PresentationCode}, {teacher.chatID})"
                );
        }

        #endregion
    }
}
