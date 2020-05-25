// SendMSN Integration for CruiseControl.NET
// Author:  Stephen R. Tunney, Canada (stephen.tunney@gmail.com)
// Date:  2005/08/01

// Date:  2008/03/04
//          - Code cleanup
//          - Added logic to go through the IntegrationResults and the modification history to see
//              who was responsible for breaking the build, and determine who should get notified.

// Date:  2009/04/23
//          - Code cleanup
//          - Elminated the need for the ConfigurationSection.  Each project should have its own unique settings.
//          - Proper use of NetReflector for all configuration items.  Much cleaner implementation now.
//          - Updated to support CCNet 1.4.3 (no compatability issues were there, but it has been tested with this version of CCNET)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Exortech.NetReflector;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Remote;
using XihSolutions.DotMSN;

namespace ThoughtWorks.CruiseControl.Plugins.Publishers.MSN
{
	[ReflectorType("msn")]
	public class MSNPublisher : ITask
	{
        private readonly ManualResetEvent m_SyncContactListCompleteEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent m_ConversationPopulatedEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent m_SignedInEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent m_AllContactsJoinedEvent = new ManualResetEvent(false);

        private readonly Messenger m_Messenger = new Messenger();                       

		private Int32		    m_ConnectionTimeout = 5000;
		private Int32		    m_ConversationTimeout = 5000;
        private long            m_JoinedContactCount = 0;
        private long            m_ExpectedContactCount = 0;
		private Hashtable	    m_Users = new Hashtable();

	    private MSNConfig       m_MSNConfig;

		#region Properties

		public Int32 ConnectionTimeout
		{
			get
			{
				return( m_ConnectionTimeout );
			}
			set
			{
				m_ConnectionTimeout = value;
			}
		}

		public Int32 ConversationTimeout
		{
			get
			{
				return( m_ConversationTimeout );
			}
			set
			{
				m_ConversationTimeout = value;
			}
		}

		[ReflectorHash( "msnusers", "name" )]
		public Hashtable MSNUsers
		{
			get
			{
				return( m_Users );
			}
			set
			{
				m_Users = value;
			}
		}

        [ReflectorProperty(@"msnconfig", Required = true)]
	    public MSNConfig MSNConfig
	    {
	        get { return m_MSNConfig; }
	        set { m_MSNConfig = value; }
	    }

	    #endregion

		public MSNUser GetMSNUser( string username )
		{
			if( null == username ) return( null );
			return( (MSNUser)m_Users[username] );
		}	    

		#region ITask Implementation

		public void Run( IIntegrationResult result )
		{
			switch( result.Status )
			{
				case IntegrationStatus.Failure:
				case IntegrationStatus.Exception:
			        string message = string.Format(@"You may have broken {0} with some recent changes.", result.ProjectName);
                    SendMessage( result, message );
					break;
                case IntegrationStatus.Success:
	    	    case IntegrationStatus.Unknown:
				default:
					//message = @"Uknown!  " + message;
					break;
			};
		}

		#endregion

        private void ResetEvents()
        {
            m_SignedInEvent.Reset();
            m_SyncContactListCompleteEvent.Reset();
            m_ConversationPopulatedEvent.Reset();
            m_AllContactsJoinedEvent.Reset();
        }

        private void SendMessage(IIntegrationResult result, string message)
		{
			try
			{
                ValidateConfiguration();

                ResetEvents();

                IList<string> sccNames = GetModificationUsers(result);

                Interlocked.Exchange(ref m_JoinedContactCount, 0);

                m_Messenger.Credentials = new Credentials(m_MSNConfig.Login, m_MSNConfig.Password, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
                m_Messenger.Nameserver.SignedIn += (Nameserver_SignedIn);
                m_Messenger.ConversationCreated += (msn_ConversationCreated);
                m_Messenger.Nameserver.SynchronizationCompleted += (Nameserver_SynchronizationCompleted);
                
                m_Messenger.Connect();                

                m_SignedInEvent.WaitOne();

                if (m_Messenger.Nameserver.IsSignedIn)
                {
                    m_Messenger.Nameserver.SynchronizeContactList();

                    m_SyncContactListCompleteEvent.WaitOne();
                    
                    Conversation conv = m_Messenger.CreateConversation();
                    conv.Switchboard.ContactJoined += Conversation_Switchboard_ContactJoinedHandler;
                    conv.Switchboard.SessionEstablished += (Switchboard_SessionEstablished);

                    IList<string> recipients = CreateNotifyList(sccNames);
                    m_ExpectedContactCount = recipients.Count;

                    foreach (string contact in recipients)
                    {
                        conv.Invite(contact);
                    }

                    if (m_AllContactsJoinedEvent.WaitOne(10000, false))
                    {
                        m_ConversationPopulatedEvent.Set();
                    }

                    bool convPopulated = m_ConversationPopulatedEvent.WaitOne(5000, false);

                    if(!convPopulated)
                    {
                        ThoughtWorks.CruiseControl.Core.Util.Log.Warning(string.Format(@"MSN Publisher::Not all recipents could be added to the conversation."));
                    }

                    if (recipients.Count != conv.Switchboard.Contacts.Count)
                    {
                        ThoughtWorks.CruiseControl.Core.Util.Log.Info(string.Format(@"MSN Publisher::Conversation not completely populated.  Population == {0}.", conv.Switchboard.Contacts.Count));
                    }

                    if (0 < conv.Switchboard.Contacts.Count)
                    {
                        conv.Switchboard.SendTextMessage(new TextMessage(message));
                    }
                }
			}
			finally
			{
                if (null != m_Messenger)
				{
                    m_Messenger.Disconnect();
				}
			}
		}

        private void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(m_MSNConfig.Login) ||
                    string.IsNullOrEmpty(m_MSNConfig.Password) ||
                    string.IsNullOrEmpty(m_MSNConfig.ScreenName))
            {
                throw (new InvalidOperationException(@"Unable to configure SendMSN.  Configuration settings are null."));
            }
        }

        private static IList<string> GetModificationUsers(IIntegrationResult result)
        {
            List<string> sccNames = new List<string>();

            foreach (Modification mod in result.Modifications)
            {
                if (!sccNames.Contains(mod.UserName))
                {
                    sccNames.Add(mod.UserName);
                }
            }

            return sccNames.AsReadOnly();
        }
        
        private void Nameserver_SynchronizationCompleted(object sender, EventArgs e)
        {
            m_Messenger.Owner.Status = PresenceStatus.Online;
            m_SyncContactListCompleteEvent.Set();
        }

        private static void msn_ConversationCreated(object sender, ConversationCreatedEventArgs e)
        {
            ThoughtWorks.CruiseControl.Core.Util.Log.Info(string.Format(@"MSN Publisher::Conversation created."));
        }

        private static void Switchboard_SessionEstablished(object sender, EventArgs e)
        {            
        }        

        private void Nameserver_SignedIn(object sender, EventArgs e)
        {
            m_SignedInEvent.Set();
        }        

        private void Conversation_Switchboard_ContactJoinedHandler(object sender, ContactEventArgs e)
        {   
            Interlocked.Increment(ref m_JoinedContactCount);
            if (m_ExpectedContactCount == Interlocked.Read(ref m_JoinedContactCount))
            {
                m_AllContactsJoinedEvent.Set();
            }
        }

		/// <summary>
		/// Takes the master list of MSNUsers specified in the configuration section
		/// and combines (while removing duplicates) the project specific MSNUsers.
		/// </summary>
		/// <param name="inclusionList">
		/// The list of names to include in the creation of this list, based on the sccname.  If this list is empty,
		/// all possible contacts are returned.
		/// </param>
		/// <returns>
		/// A string[] of email addresses.
		/// </returns>
		private IList<string> CreateNotifyList(ICollection<string> inclusionList)
		{
			List<string> userList = new List<string>();

            ThoughtWorks.CruiseControl.Core.Util.Log.Info(string.Format(@"MSN Publisher::InclusionList has {0} items.", inclusionList.Count));

		    bool isInclusionListEmpty = (0 == inclusionList.Count);

            foreach(MSNUser user in MSNUsers.Values)
			{
                //If no one was to blame then let everyone know OR let the people know who may be at fault
                if (isInclusionListEmpty ||
                    inclusionList.Contains(user.SCCName))
                {
                    userList.Add(user.EmailAddress);
                }
			}

            ThoughtWorks.CruiseControl.Core.Util.Log.Info(string.Format(@"MSN Publisher::NotifyList has {0} items.", userList.Count));

			return userList.AsReadOnly();
		}
	}
}