using System;
using System.Collections.Generic;
using System.Text;

namespace SiteMonitorService
{
	public class Settings
	{
        public Settings()
        {
            interval = 6000;
            LogToEventLog = true;
            LogToEmail = true;
            LogToMessenger = false;
            LogStateChangesOnly = true;
            LogToDb = false;
            UseSingleMSNConversation = true;
        }

        public List<string> siteUrls { get; set; }
        public int interval { get; set; }
        public bool LogToEventLog { get; set; }
        public bool LogToEmail { get; set; }
        public bool LogToMessenger { get; set; }
        public bool LogStateChangesOnly { get; set; }
        public bool LogToDb { get; set; }
        public string mailFrom { get; set; }
        public string mailTo { get; set; }
        public string mailSubject { get; set; }
        public string mailSMTPHost { get; set; }
        public string SenderPassport { get; set; }
        public string SenderPassword { get; set; }
        public string ReceiverPassport { get; set; }
        public bool UseSingleMSNConversation { get; set; }
	}
}
