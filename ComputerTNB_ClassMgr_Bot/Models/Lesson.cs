using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerTNB_ClassMgr_Bot.Models
{
    /// <summary>
    /// Lesson model.
    /// </summary>
    public class Lesson
    {
        public string lessonCode = string.Empty;
        public string lessonName = string.Empty;
        public string presentationCode = string.Empty;
        public long? teacherChatID = null;
        public DateTime lessonDateTime = DateTime.MinValue;
        public DateTime examDateTime = DateTime.MinValue;
        public string? className = null;

        public DateTime lessonEndTime = DateTime.MinValue;
    }
}
