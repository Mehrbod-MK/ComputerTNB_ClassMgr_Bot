﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerTNB_ClassMgr_Bot.Models
{
    /// <summary>
    /// Model class which represents a <see cref="Student"/> model attending a <see cref="Lesson"/>.
    /// </summary>
    public class Student_Attend
    {
        #region StudentAttend_Vairbales

        public string attendence_GUID = string.Empty;
        public long? student_ChatID = null;
        public string? student_GUID = null;
        public string? student_FullName = null;
        public string lesson_PresentationCode = string.Empty;
        public DateTime date_Attended = DateTime.MinValue;
        public long submittedBy_ChatID = 0;

        #endregion
    }
}
