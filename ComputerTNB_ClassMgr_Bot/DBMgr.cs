﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerTNB_ClassMgr_Bot.Models;
using MySql.Data.MySqlClient;

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

        public enum Roles
        {
            Unknown,

            Student,
            Teacher,
            Admin,
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

        public async Task<DBResult> SQL_GetUserRole(long chatID)
        {

        }
    }
}
