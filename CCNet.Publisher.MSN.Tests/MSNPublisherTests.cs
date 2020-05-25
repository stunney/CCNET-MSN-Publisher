using System;
using NUnit.Framework;
using ThoughtWorks.CruiseControl.Plugins.Publishers.MSN;
using System.Reflection;
using System.IO;
using XihSolutions.DotMSN;
using Rhino.Mocks;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Remote;
using System.Threading;

namespace CCNet.Publisher.MSN.Tests
{
	[TestFixture]
	public class MSNPublisherTests
	{
        private readonly ManualResetEvent m_SyncContactListCompleteEvent1 = new ManualResetEvent(false);
        private readonly ManualResetEvent m_SignedInEvent1 = new ManualResetEvent(false);

        private readonly ManualResetEvent m_SyncContactListCompleteEvent2 = new ManualResetEvent(false);
        private readonly ManualResetEvent m_SignedInEvent2 = new ManualResetEvent(false);

		public MSNPublisherTests()
		{
		}
        		
		[SetUp]
		public void SetUp()
		{
		}


		[TearDown]
		public void TearDown()
		{
		}

        private void Nameserver_SignedIn1(object sender, EventArgs e)
        {
            m_SignedInEvent1.Set();
        }

        private void Nameserver_SignedIn2(object sender, EventArgs e)
        {
            m_SignedInEvent2.Set();
        }

        private void Nameserver_SynchronizationCompleted1(object sender, EventArgs e)
        {
            ((Messenger)sender).Owner.Status = PresenceStatus.Online;
            m_SyncContactListCompleteEvent1.Set();
        }

	    private void Nameserver_SynchronizationCompleted2(object sender, EventArgs e)
        {
            ((Messenger)sender).Owner.Status = PresenceStatus.Online;
            m_SyncContactListCompleteEvent2.Set();
        }

        private static void Nameserver_ConvoCreated1(object sender, ConversationCreatedEventArgs e)
        {
            //TODO:  Assert the message gets through properly.
        }

        private static void Nameserver_ConvoCreated2(object sender, ConversationCreatedEventArgs e)
        {
            //TODO:  Assert the message gets through properly.
        }

		#region Tests

		[Test]		
		public void Test()
		{
            MockRepository mocks = new MockRepository();

			//Set up a fake build system msn account
            //Set up two fake developer msn accounts

            //ccnetsendmsnbuild1/Password123 - build server account
            //ccnetsendmsndev1/Password123 - dev1
            //ccnetsendmsndev2/Password123 - dev2

            //TODO:  RhinoMocks is too recent a version (looking for .net 3.5??)
            //TODO:  Fix bug where publisher.ConversationTimeout and ConnectionTimeout are not looking in the right place.  Should they be deleted?

            Assembly asy = Assembly.GetAssembly(typeof(MSNPublisherTests));

            Stream xmlStream = asy.GetManifestResourceStream(@"CCNet.Publisher.MSN.Tests.MSNPublisher_GoodConfig_Test.xml");

            string xml = null;

            using (StreamReader sr = new StreamReader(xmlStream))
            {
                xml = sr.ReadToEnd();
            }

            MSNPublisher publisher = (MSNPublisher)Exortech.NetReflector.NetReflector.Read(xml);

            //BUG:  Does not get settings from xml.  BOOO!!
            Assert.AreEqual(5000, publisher.ConnectionTimeout);
            //BUG:  Does not get settings from xml.  BOOO!!
            Assert.AreEqual(5000, publisher.ConversationTimeout);

            Assert.AreEqual(2000, publisher.MSNConfig.ConnectTimeout);
            Assert.AreEqual(2, publisher.MSNUsers.Count);

            Messenger dev1Msn = new Messenger();
            Messenger dev2Msn = new Messenger();
            dev1Msn.Credentials = new Credentials(@"ccnetsendmsndev1@live.com", @"Password123", Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            dev2Msn.Credentials = new Credentials(@"ccnetsendmsndev2@live.com", @"Password123", Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            dev1Msn.Nameserver.SignedIn += Nameserver_SignedIn1;
            dev1Msn.ConversationCreated += Nameserver_ConvoCreated1;
            dev1Msn.Nameserver.SynchronizationCompleted += Nameserver_SynchronizationCompleted1;

            dev2Msn.Nameserver.SignedIn += Nameserver_SignedIn2;
            dev2Msn.ConversationCreated += Nameserver_ConvoCreated2;
            dev2Msn.Nameserver.SynchronizationCompleted += Nameserver_SynchronizationCompleted2;

            try
            {
                dev1Msn.Connect();
                m_SignedInEvent1.WaitOne();
                dev1Msn.Nameserver.SynchronizeContactList();
                m_SyncContactListCompleteEvent1.WaitOne();

                dev2Msn.Connect();
                m_SignedInEvent2.WaitOne();
                dev2Msn.Nameserver.SynchronizeContactList();
                m_SyncContactListCompleteEvent2.WaitOne();
                
                Modification[] modifications = new Modification[1];
                modifications[0] = new Modification();
                modifications[0].ChangeNumber = @"1";
                modifications[0].Comment = @"Test";
                modifications[0].EmailAddress = @"ccnetsendmsndev2@live.com";
                modifications[0].FileName = @"testfile.cs";
                modifications[0].FolderName = @"//depot/main/";
                modifications[0].ModifiedTime = DateTime.Now;
                modifications[0].UserName = @"dev2";
                modifications[0].Version = "1";

                IIntegrationResult result = mocks.StrictMock<IIntegrationResult>();
                Expect.Call(result.Status).Repeat.Once().Return(IntegrationStatus.Failure);
                Expect.Call(result.ProjectName).Repeat.Once().Return(@"TestProject1");
                Expect.Call(result.Modifications).Repeat.Once().Return(modifications);

                mocks.ReplayAll();

                publisher.Run(result);

            }
            finally
            {
                mocks.VerifyAll();
            }
		}


		#endregion
	}
}
