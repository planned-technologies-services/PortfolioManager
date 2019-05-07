using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using Planned.Data;

namespace Planned.Rules
{
	public partial class TaskBusinessRules : Planned.Data.BusinessRules
    {
        
        [RowBuilder("Task", RowKind.New)]
        public void BuildNewTask()
        {
            UpdateFieldValue("TaskStatus", 'P');
        }
    }
}
