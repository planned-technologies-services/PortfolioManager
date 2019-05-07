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
	public partial class SiteCommunicationBusinessRules : Planned.Rules.SharedBusinessRules
    {
        
        [RowBuilder("SiteCommunication", RowKind.New)]
        public void BuildNewSiteCommunication()
        {
            UpdateFieldValue("MessageDate", DateTime.Now);
            UpdateFieldValue("Priority", 3);
            UpdateFieldValue("Status", 'P');
        }
    }
}
