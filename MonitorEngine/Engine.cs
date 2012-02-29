using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Configuration;
using MSNSender;
using System.Net.Mail;

namespace SiteMonitorService
{	
	public class Engine
	{

        public SiteMonitorService.Settings Settings { get; set; }
        
		System.Timers.Timer timerSM=new System.Timers.Timer();
		int _numberOfFailures=0;
		bool _firstRunOfService = true; // we want a status message (good/bad) the first time
		MessageSender _my_MsnSender = new MessageSender(); // MSN Has to stay around for the messages to stay up I guess...
		Hashtable _siteOnlineStates = new Hashtable();
		public Engine()
		{
            if (Settings == null)
            {
                Settings = new SiteMonitorService.Settings();
            }
		}

		public void BeginMonitor()
		{
			double dblInterval = Convert.ToDouble(Settings.interval);
			this.timerSM.Interval = dblInterval;
			this.timerSM.Elapsed += 
				new System.Timers.ElapsedEventHandler(timerSM_Elapsed);
			this.timerSM.Enabled = true;
		}

		public void EndMonitor()
		{
			this.timerSM.Enabled = false;
			_my_MsnSender.Close();
		}

		private void timerSM_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			this.timerSM.Enabled = false; // stop timer while we take care of this
			CheckWebPages();

			// Did they want to reset timer?
			double dblInterval = Convert.ToDouble(Settings.interval);
			this.timerSM.Interval = dblInterval;

			this.timerSM.Enabled = true; // start timer again
		}

		public void CheckWebPages()
		{
			foreach (string website in Settings.siteUrls)
			{
				CheckIndividualPage(website);
			}

			_firstRunOfService = false;
		}

		private void CheckIndividualPage(string website)
		{
			string strContent=String.Empty;
			WebResponse objResponse = null;

			if (_siteOnlineStates[website] == null)
			{
				_siteOnlineStates[website] = false;
			}

			try 
			{
				WebRequest objRequest = HttpWebRequest.Create(website);

				objResponse = objRequest.GetResponse();

				// If there is a blow up it might mean that we were unable to access the website
                using (var objSR = new StreamReader(objResponse.GetResponseStream(), System.Text.Encoding.ASCII))
                {
                }

				// if been offline, send notices that we are back
				if ((bool)_siteOnlineStates[website] && 
					Settings.LogStateChangesOnly)
				{
					if (Settings.LogToEventLog)
					{
						writeToEventLog(website + " - seems to be up" );
					}
				
					if (Settings.LogToEmail)
					{
						// An In-Box can get full if the site stays offline and we are checking very often
						SendEmail(website + " - seems to be up" );
					}
                
					if (Settings.LogToMessenger)
					{
						MessengerNotify(website + " - seems to be up" );
					}

					if (Settings.LogToDb) 
					{
						InsertDB(website, true);
					}	
				}

				_siteOnlineStates[website] = true;
			}
			catch(Exception ex)
			{
				_siteOnlineStates[website] = false;

				// if been offline, skip this - until comes back up
				if (!(bool)_siteOnlineStates[website] && !_firstRunOfService && 
					Settings.LogStateChangesOnly)
				{
					return;
				}

				_numberOfFailures++;
				Debug.WriteLine(ex.Message+ex.StackTrace);

				if (Settings.LogToEventLog)
				{
					writeToEventLog(website + " - seems to be having access problems" );
				}
				
				if (Settings.LogToEmail)
				{
					// An In-Box can get full if the site stays offline and we are checking very often
					SendEmail(website + " - seems to be having access problems" );
				}
                
				if (Settings.LogToMessenger)
				{
					MessengerNotify(website + " - seems to be having access problems" );
				}

				if (Settings.LogToDb)
				{
					InsertDB(website, false );
				}
				
			}	
		}

		void writeToEventLog(string strResult)
		{
			try
			{
				if (!EventLog.SourceExists("Application"))
				{
					EventLog.CreateEventSource("Application", "Application", ".");
				}

				EventLog objLog = new EventLog();            
				objLog.Source = "WebSite Monitor";
				objLog.WriteEntry(strResult, EventLogEntryType.Error);    
				objLog.Close();
				objLog.Dispose();			
			}
			catch // (Exception ex)
			{
				// Not good if can't log to event log, but don't just blow up!
				// throw new ApplicationException("1> Unable to Write to Event Log",ex);				 
			}
		}
		
		void SendEmail(string strResult)
		{
			if (strResult == null || strResult == string.Empty)
			{
				return;
			}

			try
			{
                if (string.IsNullOrEmpty(Settings.mailFrom) &&
                    string.IsNullOrEmpty(Settings.mailTo) &&
					string.IsNullOrEmpty(Settings.mailSubject) &&
					string.IsNullOrEmpty(Settings.mailSMTPHost))
				{
                    System.Net.Mail.MailMessage objMsg = new System.Net.Mail.MailMessage();
					objMsg.From = new MailAddress(Settings.mailFrom);
                    objMsg.To.Add(new MailAddress(Settings.mailTo));
					objMsg.Subject = Settings.mailSubject;
                    objMsg.IsBodyHtml = false;
					objMsg.Body = strResult;
                    var smtpClient = new System.Net.Mail.SmtpClient(Settings.mailSMTPHost);

                    smtpClient.Send(objMsg);
				}
			}
			catch (Exception ex)
			{
				ErrorHandler(ex.Message+ex.StackTrace+ex.InnerException.StackTrace);
			}
		}

		void MessengerNotify(string msg)
		{
			string userMail=Settings.SenderPassport;
			string userPass=Settings.SenderPassword;
			string recipients=Settings.ReceiverPassport;

			if (_my_MsnSender != null &&
				userMail != null && userMail != string.Empty &&
				userPass != null && userPass != string.Empty &&
				recipients != null && recipients != string.Empty &&
				msg != null && msg != string.Empty )
			{
				foreach (string recipient in recipients.Split(new char []{';' , ','}) )
				{
					_my_MsnSender.SendIMMessage(userMail,userPass,recipient,msg);
				}
			}
		}

		void InsertDB(string website, bool isAvailable)
		{
            //if (website == null || website == string.Empty ||
            //    Settings.connectionString == null ||
            //    Settings.connectionString == string.Empty)
            //{
            //    return;
            //}

            //try
            //{
            //    SqlConnection objConn;
            //    SqlCommand objCMD;
            //    objConn = new SqlConnection(Settings.connectionString);
            //    objCMD = new SqlCommand("uspInsertSiteMonitorStatus", objConn);
            //    objCMD.CommandType = CommandType.StoredProcedure;
            //    objCMD.Parameters.Add(new SqlParameter("@Website", SqlDbType.VarChar, 1024));
            //    objCMD.Parameters.Add(new SqlParameter("@IsAvailable", SqlDbType.Bit));
            //    objCMD.Parameters["@Website.Value = website;            
            //    objCMD.Parameters["@IsAvailable.Value = isAvailable;            
            //    objConn.Open();
            //    objCMD.ExecuteNonQuery();
            //    objConn.Close();
            //}

            //catch (Exception ex)
            //{
            //    ErrorHandler(ex.Message.ToString()+ex.StackTrace.ToString());
            //}
		}

		void ErrorHandler(string strMessage)
		{
			try
			{
				if (!EventLog.SourceExists(strMessage))
				{
					EventLog.CreateEventSource("Application", "Application", ".");
				}
				EventLog objLog = new EventLog();
				objLog.Source = "WebSite Monitor";

				strMessage = "Time " + System.DateTime.Now + " Error Message: " + strMessage;
				objLog.WriteEntry(strMessage, EventLogEntryType.Error);
				objLog.Close();
				objLog.Dispose();				
			}
			catch//(Exception BTH) // "BAD THING HAPPENED"
			{
				// Not good if can't log to event log, but don't just blow up!
				// throw new ApplicationException("2> Unable to Write to Event Log",BTH);				 
			}		
		}
	}
}
