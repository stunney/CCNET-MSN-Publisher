using System;
using NUnit.Framework;

using ThoughtWorks.CruiseControl.Plugins.Publishers.MSN;

namespace CCNet.Publisher.MSN.Tests
{
	[TestFixture]
	public class MSNUserTests
	{
		public MSNUserTests()
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
		    string sccname = @"steve";

            MSNUser user = new MSNUser(name, email, sccname);

			Assert.AreEqual( name, user.ScreenName );
			Assert.AreEqual( email, user.EmailAddress );
		}

		[Test]
		public void AreEqualTest()
		{
			string name = @"Steve";
			string email = @"steve@yahoo.ca";
            string sccname = @"steve";

            MSNUser user1 = new MSNUser(name, email, sccname);
            MSNUser user2 = new MSNUser(name, email, sccname);

			Assert.IsTrue( user1.Equals( user2 ) );
		}


		#endregion
	}
}
