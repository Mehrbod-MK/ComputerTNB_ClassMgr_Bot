using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerTNB_ClassMgr_Bot
{
    public interface IHasRole
    {
        public DBMgr.User_Roles GetRole();
    }
}
