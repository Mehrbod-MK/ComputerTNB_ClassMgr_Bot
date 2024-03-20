using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using ComputerTNB_ClassMgr_Bot.Models;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Cms;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using ZstdSharp.Unsafe;

namespace ComputerTNB_ClassMgr_Bot
{
    /// <summary>
    /// Class for managing MySQL database.
    /// </summary>
    public class DBMgr
    {
        #region DBMgr_Variables

        private string server;
        private string database;
        private string username;
        private string password;

        private string connectionString;

        private MySqlConnection sql_Connection;

        #endregion

        #region DBMgr_Structures

        #region DBMgr_Enums

        public enum User_Roles
        {
            Unknown,

            Student,
            Teacher,
            Admin,
        }

        public enum User_States : uint
        {
            At_MainMenu,

            Teacher_Viewing_Lessons,

            Teacher_Checking_Lesson_Attendence,
        }

        #endregion

        /// <summary>
        /// Represents a database function call result.
        /// </summary>
        public sealed class DBResult
        {
            public bool success = false;

            public object? result = null;
            public Exception? exception = null;
        }

        #endregion

        #region DBMgr_Generics

        public static T? ConvertFromDBVal<T>(object obj)
        {
            if (obj == null || obj == DBNull.Value)
            {
                return default(T); // returns the default value for the type
            }
            else
            {
                return (T)obj;
            }
        }

        #endregion

        #region DBMgr_Properties

        public string ServerName
        {
            set { server = value; }
            get { return server; }
        }

        public string Database
        {
            set { database = value; }
            get { return database; }
        }

        public string UserName
        { 
            set { username = value; }
            get { return username; } 
        }

        public string Password
        {
            set { password = value; }
            get { return password; }
        }

        public string ConnectionString
        {
            get { return connectionString; }
        }

        #endregion

        /// <summary>
        /// Default Ctor.
        /// </summary>
        public DBMgr
            (string server, string database, string username, string password)
        {
            // Initialize members.
            this.server = server;
            this.database = database;
            this.username = username;
            this.password = password;

            // Generate connection string.
            connectionString = 
                $"SERVER={server};DATABASE={database};UID={username};PASSWORD={password};";
        }

        public void DBMS_TestConnection()
        {
            using(var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                connection.Close();
            }
        }

        /// <summary>
        /// Asynchronously opens and closes connection for testing.
        /// </summary>
        public async Task DBMS_TestConnectionAsync()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.ConfigureAwait(false);

                await connection.OpenAsync();
                await connection.CloseAsync();
            }
        }

        /// <summary>
        /// This method retrieves a Student object from students' table, providing its ChatID.
        /// </summary>
        /// <param name="chatID">Primary key:  ChatID.</param>
        /// <returns>This task returns a DBResult structure.</returns>
        public async Task<DBResult> SQL_GetStudent(long chatID)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    string commandText = "SELECT * FROM students " +
                        "WHERE ChatID = @CHAT_ID";

                    MySqlCommand command =
                        new MySqlCommand(commandText, connection);
                    command.Parameters.AddWithValue("CHAT_ID", chatID);
                    command.ConfigureAwait(false);

                    var reader = command.ExecuteReader();
                    reader.ConfigureAwait(false);

                    Student? student = null;
                    if (await reader.ReadAsync())
                    {
                        student = new Student()
                        {
                            chatID = (long)reader["ChatID"],
                            email = ConvertFromDBVal<string?>(reader["Email"]),
                            firstName = ConvertFromDBVal<string?>(reader["FirstName"]),
                            lastName = ConvertFromDBVal<string?>(reader["LastName"]),
                            joinedDate = (DateTime)reader["JoinDate"],
                            lastActivity = (DateTime)reader["LastActivity"],
                            phoneNumber = ConvertFromDBVal<string?>(reader["PhoneNumber"]),
                            studentId = ConvertFromDBVal<string?>(reader["StudentID"]),

                            state = (uint)reader["State"],
                        };
                    }

                    await reader.CloseAsync();
                    await command.DisposeAsync();

                    return new DBResult()
                    {
                        success = true,
                        result = student,
                    };
                }
            }
            catch(Exception ex)
            {
                return new DBResult()
                { exception = ex, result = null, success = false };
            }
        }

        /// <summary>
        /// This method retrieves a Teacher object from teachers' table, providing its ChatID.
        /// </summary>
        /// <param name="chatID">Primary key:  ChatID.</param>
        /// <returns>This task returns a DBResult structure.</returns>
        public async Task<DBResult> SQL_GetTeacher(long chatID)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    string commandText = "SELECT * FROM teachers " +
                        "WHERE ChatID = @CHAT_ID";

                    MySqlCommand command =
                        new MySqlCommand(commandText, connection);
                    command.Parameters.AddWithValue("CHAT_ID", chatID);
                    command.ConfigureAwait(false);

                    var reader = command.ExecuteReader();
                    reader.ConfigureAwait(false);

                    Teacher? teacher = null;
                    if (await reader.ReadAsync())
                    {
                        teacher = new Teacher()
                        {
                            chatID = (long)reader["ChatID"],
                            email = ConvertFromDBVal<string?>(reader["Email"]),
                            fullName = (string)reader["FullName"],
                            joinedDate = (DateTime)reader["JoinDate"],
                            lastActivity = (DateTime)reader["LastActivity"],
                            nationalID = (string)reader["NationalID"],
                            phoneNumber = ConvertFromDBVal<string?>(reader["PhoneNumber"]),

                            state = (uint)reader["State"],

                            __meta = ConvertFromDBVal<string?>(reader["__meta"]),
                        };
                    }

                    await reader.CloseAsync();
                    await command.DisposeAsync();

                    return new DBResult()
                    {
                        success = true,
                        result = teacher,
                    };
                }
            }
            catch (Exception ex)
            {
                return new DBResult()
                { exception = ex, result = null, success = false };
            }
        }

        /// <summary>
        /// This method retrieves a Admin object from admins' table, providing its ChatID.
        /// </summary>
        /// <param name="chatID">Primary key:  ChatID.</param>
        /// <returns>This task returns a DBResult structure.</returns>
        public async Task<DBResult> SQL_GetAdmin(long chatID)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    string commandText = "SELECT * FROM admins " +
                        "WHERE ChatID = @CHAT_ID";

                    MySqlCommand command =
                        new MySqlCommand(commandText, connection);
                    command.Parameters.AddWithValue("CHAT_ID", chatID);
                    command.ConfigureAwait(false);

                    var reader = command.ExecuteReader();
                    reader.ConfigureAwait(false);

                    Admin? admin = null;
                    if (await reader.ReadAsync())
                    {
                        admin = new Admin()
                        {
                            chatID = (long)reader["ChatID"],
                            joinedDate = (DateTime)reader["JoinDate"],
                            lastActivity = (DateTime)reader["LastActivity"],

                            can_View_Students = (bool)reader["Can_View_Students"],
                            can_View_Teachers = (bool)reader["Can_View_Teachers"],

                            state = (uint)reader["State"],
                        };
                    }

                    await reader.CloseAsync();
                    await command.DisposeAsync();

                    return new DBResult()
                    {
                        success = true,
                        result = admin,
                    };
                }
            }
            catch (Exception ex)
            {
                return new DBResult()
                { exception = ex, result = null, success = false };
            }
        }

        /// <summary>
        /// Gets the role of user, providing its ChatID.
        /// </summary>
        /// <param name="chatID">Primary Key: ChatID.</param>
        /// <returns>This task returns a role enumeration.</returns>
        public async Task<User_Roles> SQL_GetUserRole(long chatID)
        {
            var db_Result = await SQL_GetAdmin(chatID);
            if (db_Result.result != null)
                return ((Admin)db_Result.result).GetRole();

            db_Result = await SQL_GetTeacher(chatID);
            if (db_Result.result != null)
                return ((Teacher)db_Result.result).GetRole();

            db_Result = await SQL_GetStudent(chatID);
            if (db_Result.result != null)
                return ((Student)db_Result.result).GetRole();

            return User_Roles.Unknown;
        }

        /// <summary>
        /// This task executes a WRITE command on MySql Database.
        /// </summary>
        /// <param name="command">The SQL command to execute.</param>
        /// <returns>This task returns a database result structure.</returns>
        public async Task<DBResult> SQL_ExecuteWrite(string command)
        {
            try
            {
                var rowsAffected = -1;

                using(var connection = new MySqlConnection(ConnectionString))
                {
                    connection.ConfigureAwait(false);

                    // Open SQL connection.
                    connection.Open();

                    // Create transaction.
                    MySqlTransaction writeTransaction = await connection.BeginTransactionAsync();
                    writeTransaction.ConfigureAwait(false);

                    try
                    {
                        // Create command.
                        MySqlCommand writeCmd = new MySqlCommand(command, connection, writeTransaction);
                        writeCmd.ConfigureAwait(false);

                        // Execute write command and dispose it.
                        rowsAffected = await writeCmd.ExecuteNonQueryAsync();
                        await writeCmd.DisposeAsync();

                        // Commit transaction and dispose it.
                        await writeTransaction.CommitAsync();
                        await writeTransaction.DisposeAsync();

                        return new DBResult()
                        {
                            success = true,
                            exception = null,
                            result = rowsAffected,
                        };
                    }
                    catch(Exception)
                    {
                        try
                        {
                            // Roll back changes.
                            await writeTransaction.RollbackAsync();

                            return new DBResult()
                            {
                                success = false,
                                exception = null,
                                result = -1,
                            };
                        }
                        catch(Exception)
                        {
                            // Throw fatal exception.
                            throw;
                        }
                    }

                }
            }
            catch(Exception ex)
            {
                return new DBResult()
                {
                    success = false,
                    result = null,
                    exception = ex,
                };
            }
        }
        
        public static string Convert_FromDateTime_ToSQLString(DateTime dateTime)
        {
            return
                $"{dateTime.Year:0000}-{dateTime.Month:00}-{dateTime.Day:00} " +
                $"{dateTime.Hour}:{dateTime.Minute}:{dateTime.Second}";
        }

        public static string Convert_FromDateTime_ToPersianLongDateTimeString(DateTime dateTime)
        {
            PersianCalendar pc = new PersianCalendar();

            string nameOfDay = string.Empty;
            string nameOfMonth = string.Empty;

            var dayOfWeek = pc.GetDayOfWeek(dateTime);
            switch(dayOfWeek)
            {
                case DayOfWeek.Saturday:    nameOfDay = "شنبه"; break;
                case DayOfWeek.Sunday:      nameOfDay = "یکشنبه"; break;
                case DayOfWeek.Monday:      nameOfDay = "دوشنبه"; break;
                case DayOfWeek.Tuesday:     nameOfDay = "سه‌شنبه"; break;
                case DayOfWeek.Wednesday:   nameOfDay = "چهارشنبه"; break;
                case DayOfWeek.Thursday:    nameOfDay = "پنجشنبه"; break;
                case DayOfWeek.Friday:      nameOfDay = "جمعه"; break;
            }

            var monthInYear = pc.GetMonth(dateTime);
            switch(monthInYear)
            {
                case 1: nameOfMonth = "فروردین"; break;
                case 2: nameOfMonth = "اردیبهشت"; break;
                case 3: nameOfMonth = "خرداد"; break;

                case 4: nameOfMonth = "تیر"; break;
                case 5: nameOfMonth = "مرداد"; break;
                case 6: nameOfMonth = "شهریور"; break;

                case 7: nameOfMonth = "مهر"; break;
                case 8: nameOfMonth = "آبان"; break;
                case 9: nameOfMonth = "آذر"; break;

                case 10: nameOfMonth = "دی"; break;
                case 11: nameOfMonth = "بهمن"; break;
                case 12: nameOfMonth = "اسفند"; break;
            }

            return
                $"{nameOfDay}، {pc.GetDayOfMonth(dateTime):00} {nameOfMonth} {pc.GetYear(dateTime):0000} - " +
                $"ساعت {pc.GetHour(dateTime):00}:{pc.GetMinute(dateTime)}:{pc.GetSecond(dateTime):00}";
        }

        /// <summary>
        /// This method retrieves a <see cref="List{T}"/> models bound to a <see cref="Teacher"/>, providing its teacher ID.
        /// </summary>
        /// <param name="chatID">Primary key:  Teacher_ChatID.</param>
        /// <returns>This task returns a <see cref="DBResult"/> structure.</returns>
        public async Task<DBResult> SQL_GetListOfTeacherLessons(long teacherID)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    string commandText = "SELECT * FROM lessons " +
                        "WHERE TeacherChatID = @TEACHER_CHAT_ID";

                    MySqlCommand command =
                        new MySqlCommand(commandText, connection);
                    command.Parameters.AddWithValue("TEACHER_CHAT_ID", teacherID);
                    command.ConfigureAwait(false);

                    var reader = command.ExecuteReader();
                    reader.ConfigureAwait(false);

                    List<Lesson> teacherLessons = new();
                    while(await reader.ReadAsync())
                    {
                        Lesson lesson = new();

                        lesson = new Lesson()
                        {
                            lessonCode = (string)reader["LessonCode"],
                            lessonName = (string)reader["lessonName"],
                            presentationCode = (string)reader["PresentationCode"],
                            teacherChatID = ConvertFromDBVal<long?>(reader["TeacherChatID"]),
                            lessonDateTime = (DateTime)reader["LessonDateTime"],
                            examDateTime = (DateTime)reader["ExamDateTime"],
                            className = ConvertFromDBVal<string?>(reader["ClassName"]),

                            lessonEndTime = (DateTime)reader["LessonEndTime"],
                        };

                        teacherLessons.Add(lesson);
                    }

                    await reader.CloseAsync();
                    await command.DisposeAsync();

                    return new DBResult()
                    {
                        success = true,
                        result = teacherLessons,
                    };
                }
            }
            catch (Exception ex)
            {
                return new DBResult()
                { exception = ex, result = null, success = false };
            }
        }

        public async Task<DBResult> SQL_GetLesson(string presentCode)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    string commandText = "SELECT * FROM lessons " +
                        "WHERE PresentationCode = @LESSON_PRESENT_CODE";

                    MySqlCommand command =
                        new MySqlCommand(commandText, connection);
                    command.Parameters.AddWithValue("LESSON_PRESENT_CODE", presentCode);
                    command.ConfigureAwait(false);

                    var reader = command.ExecuteReader();
                    reader.ConfigureAwait(false);

                    Lesson? lesson = null;
                    if (await reader.ReadAsync())
                    {
                        lesson = new Lesson()
                        {
                            lessonCode = (string)reader["LessonCode"],
                            lessonName = (string)reader["lessonName"],
                            presentationCode = (string)reader["PresentationCode"],
                            teacherChatID = ConvertFromDBVal<long?>(reader["TeacherChatID"]),
                            lessonDateTime = (DateTime)reader["LessonDateTime"],
                            examDateTime = (DateTime)reader["ExamDateTime"],
                            className = ConvertFromDBVal<string?>(reader["ClassName"]),
                            
                            lessonEndTime = (DateTime)reader["LessonEndTime"],
                        };
                    }

                    await reader.CloseAsync();
                    await command.DisposeAsync();

                    return new DBResult()
                    {
                        success = true,
                        result = lesson,
                    };
                }
            }
            catch (Exception ex)
            {
                return new DBResult()
                { exception = ex, result = null, success = false };
            }
        }

        public async Task<DBResult> SQL_GetListOfLessonStudents(string presentationCode)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    string commandText = "SELECT * FROM students_x_lessons " +
                        "WHERE Class_ID = @PRESENT_CODE";

                    MySqlCommand command =
                        new MySqlCommand(commandText, connection);
                    command.Parameters.AddWithValue("PRESENT_CODE", presentationCode);
                    command.ConfigureAwait(false);

                    var reader = command.ExecuteReader();
                    reader.ConfigureAwait(false);

                    List<Student> lessonStudents = new();
                    while (await reader.ReadAsync())
                    {
                        var studentQuery = await this.SQL_GetStudent(
                            (long)reader["Student_ChatID"]
                            );

                        if (studentQuery.result == null)
                            continue;

                        lessonStudents.Add((Student)studentQuery.result);
                    }

                    await reader.CloseAsync();
                    await command.DisposeAsync();

                    return new DBResult()
                    {
                        success = true,
                        result = lessonStudents,
                    };
                }
            }
            catch (Exception ex)
            {
                return new DBResult()
                { exception = ex, result = null, success = false };
            }
        }

        /// <summary>
        /// Updates User State in database.
        /// </summary>
        /// <param name="teacher">The <see cref="Teacher"/> object to update its state.</param>
        /// <param name="state">The target state.</param>
        /// <returns>This task returns a <see cref="DBResult"/> structure.</returns>
        /// <exception cref="ApiRequestException"></exception>
        public async Task<DBResult> SQL_Set_User_State(Teacher teacher, uint state)
        {
            // Check if there is no need for updating...
            if (teacher.state == state)
                return new DBResult() { success = true, };

            var result = await SQL_ExecuteWrite($"UPDATE teachers " +
                $"SET State = {state} " +
                $"WHERE ChatID = {teacher.chatID};");

            if(result.success == false)
            {
                if (result.exception != null)
                    throw result.exception;
                else
                    throw new ApiRequestException("وضعیت استاد در پایگاه داده به روز نشد!");
            }

            return result;
        }

        public static string Convert_FromDateTime_ToLessonDateTimeLongString(Lesson lesson)
        {
            var time_StartLesson = lesson.lessonDateTime;
            var time_EndLesson = lesson.lessonEndTime;

            PersianCalendar pc = new PersianCalendar();

            var dayOfWeek = pc.GetDayOfWeek(time_StartLesson);
            string str_DayOfWeek = string.Empty;
            switch(dayOfWeek)
            {
                case DayOfWeek.Sunday:
                    str_DayOfWeek += "1شنبه";
                    break;

                case DayOfWeek.Monday:
                    str_DayOfWeek += "2شنبه";
                    break;

                case DayOfWeek.Tuesday:
                    str_DayOfWeek += "3شنبه";
                    break;

                case DayOfWeek.Wednesday:
                    str_DayOfWeek += "4شنبه";
                    break;

                case DayOfWeek.Thursday:
                    str_DayOfWeek += "5شنبه";
                    break;

                case DayOfWeek.Friday:
                    str_DayOfWeek += "6جمعه";
                    break;

                case DayOfWeek.Saturday:
                    str_DayOfWeek += "0شنبه";
                    break;

            }

            string startTime = $"{pc.GetHour(time_StartLesson):00}:{pc.GetMinute(time_StartLesson)}";
            string endTime = $"{pc.GetHour(time_EndLesson):00}:{pc.GetMinute(time_EndLesson)}";

            return $"{str_DayOfWeek}، از {startTime} الی {endTime}";
        }

        public static bool Check_TimeIsCurrentForAttendence(DateTime startOfLesson, DateTime endOfLesson)
        {
            var now = DateTime.Now;

            // Check day of week.
            if (now.DayOfWeek != startOfLesson.DayOfWeek)
                return false;

            // Check time span.
            long startSeconds = startOfLesson.Hour * 3600 + startOfLesson.Minute * 60 + startOfLesson.Second;
            long endSeconds = endOfLesson.Hour * 3600 + endOfLesson.Minute * 60 + endOfLesson.Second;
            long nowSeconds = now.Hour * 3600 + now.Minute * 60 + now.Second;

            if (!(nowSeconds >= startSeconds && nowSeconds <= endSeconds))
                return false;

            return true;
        }

        /// <summary>
        /// Updates User's META data.
        /// </summary>
        /// <param name="teacher">The <see cref="Teacher"/> object to update its metadata.</param>
        /// <param name="state">The target metadatas.</param>
        /// <returns>This task returns a <see cref="DBResult"/> structure.</returns>
        public async Task<DBResult> SQL_Set_User_MetaData(Teacher teacher, string? metaData)
        {
            DBResult result;

            if (string.IsNullOrEmpty(metaData))
                result = await SQL_ExecuteWrite($"UPDATE teachers " +
                    $"SET __META = NULL" +
                    $"WHERE ChatID = {teacher.chatID}");
            else
                result = await SQL_ExecuteWrite($"UPDATE teachers " +
                    $"SET __META = \'{metaData}\' " +
                    $"WHERE ChatID = {teacher.chatID};");

            if (result.success == false)
            {
                if (result.exception != null)
                    throw result.exception;
                else
                    throw new ApiRequestException("وضعیت استاد در پایگاه داده به روز نشد!");
            }

            return result;
        }

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="string"/>s representing file paths to images for AI model training.
        /// </summary>
        /// <param name="ai_PersonModelIndex">The AI model index associated with a person in DB. If set to <see cref="null"/>, then all images will be returned.</param>
        /// <returns></returns>
        public async Task<DBResult> SQL_Get_ListOfImagesPaths(uint? ai_PersonModelIndex = null)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.ConfigureAwait(false);
                    await connection.OpenAsync();

                    string commandText = string.Empty;
                    if (ai_PersonModelIndex != null)
                        commandText = "SELECT * FROM images " +
                            "WHERE PresentationCode = @AI_MODEL_INDEX";
                    else
                        commandText = "SELECT * FROM images";

                    MySqlCommand command =
                        new MySqlCommand(commandText, connection);
                    if (ai_PersonModelIndex != null)
                        command.Parameters.AddWithValue("AI_MODEL_INDEX", ai_PersonModelIndex);
                    command.ConfigureAwait(false);

                    var reader = command.ExecuteReader();
                    reader.ConfigureAwait(false);

                    List<AI_ImageIndex> imageIndices = new List<AI_ImageIndex>();
                    if (await reader.ReadAsync())
                    {
                        AI_ImageIndex imgIdx = new AI_ImageIndex()
                        {
                            imagePath = (string)reader["Image_RelativePath"],
                            ai_ModelIndex = (int)reader["AI_ModelIndex"],
                        };

                        imageIndices.Add(imgIdx);
                    }

                    await reader.CloseAsync();
                    await command.DisposeAsync();

                    return new DBResult()
                    {
                        success = true,
                        result = imageIndices,
                    };
                }
            }
            catch (Exception ex)
            {
                return new DBResult()
                { exception = ex, result = null, success = false };
            }
        }

        /// <summary>
        /// Retrieves a globally unique identifier (GUID), for unique objects in time.
        /// </summary>
        /// <returns>This method returns a standardized FileName <see cref="string"/>.</returns>
        public static string _GET_GUID()
        {
            var guid = Guid.NewGuid();
            var str = Convert.ToBase64String(guid.ToByteArray());
            return string.Join(string.Empty, str.Split(Path.GetInvalidFileNameChars()));
        }

        /// <summary>
        /// Downloads a <see cref="Telegram.Bot.Types.PhotoSize"/> and submits it to local directory structure.
        /// </summary>
        /// <param name="botClient">Telegram bot client to download the file from.</param>
        /// <param name="photo">The received Photo object.</param>
        /// <param name="directory">Relative directory to store the image to.</param>
        /// <returns>This task returns a <see cref="DBResult"/> structure. <see cref="DBResult.result"/> contains the absolute path of the submitted file.</returns>
        public async Task<DBResult> FILE_Photo_SubmitToDirectory(TelegramBotClient botClient, Telegram.Bot.Types.PhotoSize photo, string directory = "photos")
        {
            try
            {
                // Validate Unique FileName.
                string fileName = string.Join("_", photo.FileUniqueId.Split(Path.GetInvalidFileNameChars()));

                // Create directory.
                Directory.CreateDirectory(directory);

                // Create new unique file. (+ EXTENSION?)
                FileStream fstream = new FileStream(
                    Path.Combine(new string[] { directory, fileName }), FileMode.Create, FileAccess.ReadWrite
                    );
                string finalFileName = fstream.Name;

                // Download and submit file.
                await botClient.GetInfoAndDownloadFileAsync(photo.FileId, fstream);

                // Close file handle.
                fstream.Close();
                fstream.Dispose();

                return new DBResult()
                {
                    result = finalFileName,
                    success = true,
                    exception = null,
                };
            }
            catch(Exception ex)
            {
                return new DBResult()
                { success = false, result = null, exception = ex };
            }
        }
    }
}
