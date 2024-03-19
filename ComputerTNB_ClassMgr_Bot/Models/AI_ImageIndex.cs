using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerTNB_ClassMgr_Bot.Models
{
    /// <summary>
    /// A simple model for stroing an image path and an integer, representing person's AI model index in database.
    /// </summary>
    public class AI_ImageIndex
    {
        #region AI_ImageIndex_Variables

        public string imagePath = string.Empty;
        public int ai_ModelIndex = 0;

        #endregion
    }
}
