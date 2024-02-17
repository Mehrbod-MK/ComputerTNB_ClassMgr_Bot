using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerTNB_ClassMgr_Bot.Models
{
    /// <summary>
    /// Model class for a Teacher database object.
    /// </summary>
    public class Teacher
    {
        #region Teacher_Variables

        public long chatID = 0;
        public string? phoneNumber = null;
        public string? email = null;
        public DateTime joinedDate = DateTime.MinValue;
        public DateTime lastActivity = DateTime.MinValue;
        public string nationalID = string.Empty;
        public string fullName = string.Empty;

        public uint state = 0;

        #endregion

        #region Teacher_Implementations

        public DBMgr.User_Roles GetRole()
        { return DBMgr.User_Roles.Teacher; }

        #endregion
    }
}
