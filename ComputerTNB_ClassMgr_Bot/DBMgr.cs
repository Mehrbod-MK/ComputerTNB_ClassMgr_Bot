﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // Create MySQL Connection object.
            this.sql_Connection = new MySqlConnection(this.connectionString);
        }

        public void DBMS_TestConnection()
        {
            this.sql_Connection.Open();      
            this.sql_Connection.Close();
        }

        /// <summary>
        /// Asynchronously opens and closes connection for testing.
        /// </summary>
        public async Task DBMS_TestConnectionAsync()
        {
            await this.sql_Connection.OpenAsync().ConfigureAwait(false);
            await this.sql_Connection.CloseAsync().ConfigureAwait(false);
        }
    }
}