using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerTNB_ClassMgr_Bot.Models
{
    /// <summary>
    /// Model class for a Student in database.
    /// </summary>
    public class Student
    {
        #region Student_Variables

        public long chatID = 0;
        public string? phoneNumber = null;
        public string? email = null;
        public DateTime joinedDate = DateTime.MinValue;
        public DateTime lastActivity = DateTime.MinValue;
        public string? studentId = null;
        public string? firstName = null;
        public string? lastName = null;

        #endregion
    }
}
