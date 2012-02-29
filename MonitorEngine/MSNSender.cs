using System;
using DotMSN;
using System.Configuration;
using System.Collections;

namespace MSNSender
{
	public class MessageSender
	{
		private DotMSN.Messenger _messenger = null;
		private string _internalLog = String.Empty;
		private string _messageToSend;
		private ArrayList _selectedContacts = new ArrayList();
		private bool _firstContactJoined = false, _useSingleConversation = false;  

		public MessageSender()
		{
			try
			{
				_messenger = new DotMSN.Messenger();	
			} 
			catch 
			{
				throw new ApplicationException("Unable to connect. Your Internet connection might be down");
			}

			_messenger.ContactOnline += new Messenger.ContactOnlineHandler(ContactOnline);		
			// notify us when synchronization is completed
			_messenger.SynchronizationCompleted += new Messenger.SynchronizationCompletedHandler(OnSynchronizationCompleted);
			_messenger.ConversationCreated += new Messenger.ConversationCreatedHandler(ConversationCreated);
			
			if (ConfigurationManager.AppSettings["UseSingleMSNConversation"] != null &&
				ConfigurationManager.AppSettings["UseSingleMSNConversation"].ToUpper() == "TRUE")
			{
				_useSingleConversation = true;
			}
		}

		public void SendIMMessage(string userMail,string userPassword,string contactMail, string strMessage)
		{
			if (_messenger == null)
			{
				return;
			}
	
			this._messageToSend = strMessage;

			if (!_messenger.Connected)
			{
				try
				{
					_messenger.Connect(userMail, userPassword);	
				} 
				catch {
					throw new ApplicationException("\r\nUnable to connect!\r\nYour Internet connection might be down.\r\nOr the credentials may be incorrect for: " + userMail + "\r\n");
				}

				_messenger.SynchronizeList();
			}
			
			foreach (string personToContact in contactMail.Split(new char []{';' , ','}) )
			{
				// don't want to try to contact self - wont work anyway.
				// BTW: if you are already logged on, you will be logged off
				//		since we are logging on again in this class :)
				if(personToContact != String.Empty && personToContact != userMail)
				{
					this._internalLog  += "Contacting > " + personToContact + "\r\n";
					
					_selectedContacts.Add(personToContact);

				}
			}
		}

		private void ConnectionEstablished(Conversation sender, EventArgs e)
		{
			this._internalLog +=   "connection to IM established.\r\n";
		}

		private void OnSynchronizationCompleted(Messenger sender, EventArgs e)
		{
			// now set our initial status (Required) - do this before requesting conv
			_messenger.SetStatus(MSNStatus.Online);					
			this._internalLog  += "Status set to online!\r\n";

			DotMSN.Conversation conv;

			// start multiple conversations 
			// (or start one and ask others to join after it is 100% up!)
			foreach (string personToContact in _selectedContacts)
			{
				try
				{
					// start a new conversation with each person (unless useSingle is true)
					conv = _messenger.RequestConversation(personToContact);

					// remember there are not yet users in the conversation (except ourselves)
					// they will join _after_ this event. We create another callback to handle this.
					// When user(s) have joined we can start sending messages.

					// Use this if want to use a single conversation
					// all the rest will fail though, if this contact is offline!
					if (_useSingleConversation)
					{
						break;	
					}
				} 
				catch {} // if this fails, someone doesn't get notified... 
				// that's probably less problematic than dropping everything and blowing up :)
			}

			/* cool - info about contacts - but not needed!

			// enumerate contacts and see who we might be able to talk to...
			// This assumes the folks we are contacting are in our contact list
			// - but we already went through the trouble of inviting contacts directly.
			foreach(Contact contact in _messenger.GetListEnumerator(MSNList.ForwardList))
			{
				if(_selectedContacts.IndexOf(contact.Mail) > -1 )
				{
					_internalLog  += "To Notify > " + contact.Name + " (" + contact.Status + ")\r\n";
				}
			}

			foreach(Contact contact in messenger.ReverseList)
			{
				this.Log  += "RL > " + contact.Name + " (" + contact.Status + ")\r\n";
			}

			foreach(Contact contact in messenger.BlockedList)
			{
				this.Log  += "BL > " + contact.Name + " (" + contact.Status + ")\r\n";
			}

			// when the privacy of the client is set to MSNPrivacy.NoneButAllowed then only
			// the contacts on the allowedlist are able to see your status
			foreach(Contact contact in messenger.AllowedList)
			{
				this.Log  += "AL > " + contact.Name + " (" + contact.Status + ")\r\n";
			}
			*/
		}
	 
		private void ConversationCreated(Messenger sender, ConversationEventArgs e)
		{
			// we request a conversation or were asked one. Now log this
			_internalLog  += "Conversation object created\r\n";

			e.Conversation.ContactJoin += new Conversation.ContactJoinHandler(ContactJoined);			
			// log the event when the two clients are connected
			e.Conversation.ConnectionEstablished += new Conversation.ConnectionEstablishedHandler(ConnectionEstablished);
		}
 
		private void ContactJoined(Conversation sender, ContactEventArgs e)
		{
			// someone joined our conversation! remember that this also occurs when you are
			// only talking to 1 other person. Log this event.
			if (_selectedContacts.IndexOf(e.Contact.Mail) > -1) // one of ours
			{
				_internalLog += e.Contact.Name + " joined the conversation.\r\n";

				// now send it. You can send messages using the Conversation object.
				sender.SendMessage( this._messageToSend );
			}


			// If using a single conversation, need to have the first one fully
			// established, connected and joined before we can invite others
			// THE ONLY PROBLEM IS THAT IF THAT FIRST CONTACT IS OFFLINE, 
			// THEN WE'LL NEVER GET HERE TO INVITE OTHERS
			if (!_firstContactJoined && _useSingleConversation )
			{
				_firstContactJoined = true; // so we only do this once!

				// if using a single conversation, then invite other contacts now
				foreach (string personToContact in _selectedContacts)
				{
					if (e.Contact.Mail != personToContact) // not the first one we contacted
					{
						sender.Invite(this._messenger.GetContact(personToContact));
					}
				}
			}
		}

		private void ContactOnline(Messenger sender, ContactEventArgs e)
		{
			// just a string :) not really doing anything with this, but it's cool huh?
			//	- we can write our own messenger client even!
			// _internalLog += e.Contact.Name + " went online\r\n";
		}

		public void Close()
		{
			if (_messenger != null && _messenger.Connected)
			{
				_firstContactJoined = false;

				_messenger.CloseConnection();
			}
		}
	}
}
