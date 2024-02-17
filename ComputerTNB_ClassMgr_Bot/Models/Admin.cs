using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerTNB_ClassMgr_Bot.Models
{
    internal class Admin
    {
        #region Admin_Variables

        public long chatID = 0;
        public DateTime joinedDate = DateTime.MinValue;
        public DateTime lastActivity = DateTime.MinValue;

        public bool can_View_Teachers = true;
        public bool can_View_Students = true;

        public uint state = 0;

        #endregion

        #region Admin_Implementations

        public DBMgr.User_Roles GetRole()
        { return DBMgr.User_Roles.Admin; }

        #endregion
    }
}
