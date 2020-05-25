// SendMSN Integration for CruiseControl.NET
// Author:  Stephen R. Tunney, Canada (stephen.tunney@gmail.com)
// Date:  2005/08/01

// Update:  2008/03/04
//          Added SCCName as a link between an MSN User and a source code control user for sending
//          blame to a specific person/contact.

using Exortech.NetReflector;

namespace ThoughtWorks.CruiseControl.Plugins.Publishers.MSN
{
	[ReflectorType("msnuser")]
	public class MSNUser
	{
		private string		m_ScreenName;
	    private string      m_SCCName;
		private string		m_EmailAddress;

		public MSNUser()
		{
		}

		public MSNUser( string screenName, string emailAddress, string sccName )
		{
			m_ScreenName = screenName;
			m_EmailAddress = emailAddress;
		    m_SCCName = sccName;
		}

		[ReflectorProperty("name")]
		public string ScreenName
		{
			get
			{
				return( m_ScreenName );
			}
			set
			{
				m_ScreenName = value;
			}
		}
		
		[ReflectorProperty("email")]
		public string EmailAddress
		{
			get
			{
				return( m_EmailAddress );
			}
			set
			{
				m_EmailAddress = value;
			}
		}

        [ReflectorProperty("sccname")]
	    public string SCCName
	    {
	        get { return m_SCCName; }
	        set { m_SCCName = value; }
	    }

	    public override int GetHashCode()
		{
			return( string.Concat( EmailAddress, ScreenName ).GetHashCode() );
		}

		public override string ToString()
		{
			return( string.Format( @"{0} <{1}>", ScreenName, EmailAddress ) );
		}

		public bool Equals( MSNUser user )
		{
			if( user == null || user.GetType() != this.GetType() )
			{
				return false;
			}

			return( user.EmailAddress.Equals( EmailAddress ) && 
					user.ScreenName.Equals( ScreenName ) &&
                    user.m_SCCName.Equals(SCCName));
		}
	}
}