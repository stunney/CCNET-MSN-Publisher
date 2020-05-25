using System;
using NUnit.Framework;

using ThoughtWorks.CruiseControl.Plugins.Publishers.MSN;

namespace CCNet.Publisher.MSN.Tests
{
	[TestFixture]
	public class MSNConfigTests
	{
		public MSNConfigTests()
		{
		}

		#region NUnit Methods

		[SetUp]
		public void SetUp()
		{
		}


		[TearDown]
		public void TearDown()
		{
		}


		#endregion

		#region Tests

		[Test]
		public void ConstructorTest()
		{
			string name = @"Steve";
			string email = @"steve@yahoo.ca";

			MSNConfig conf = new MSNConfig( 100, 200, name, email, "password" );
			
			Assert.AreEqual( 100, conf.ConnectTimeout );
			Assert.AreEqual( 200, conf.ConversationTimeout );
			Assert.AreEqual( name, conf.ScreenName );
			Assert.AreEqual( email, conf.Login );
			Assert.AreEqual( "password", conf.Password );
		}


		#endregion
	}
}
