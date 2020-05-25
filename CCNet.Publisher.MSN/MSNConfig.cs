// SendMSN Integration for CruiseControl.NET
// Author:  Stephen R. Tunney, Canada (stephen.tunney@gmail.com)
// Date:  2005/08/01

using System;
using Exortech.NetReflector;

namespace ThoughtWorks.CruiseControl.Plugins.Publishers.MSN
{
	/// <summary>
	/// Summary description for MSNConfig.
	/// </summary>
	[ReflectorType("msnconfig")]	
	public class MSNConfig
	{
		private Int32 _connectTimeout;
		private Int32 _conversationTimeout;
		private string _screenName;
		private string _login;
		private string _password;

		public MSNConfig() {}

		public MSNConfig( Int32 connectTimeout,
			Int32 conversationTimeout,
			string screenName,
			string login,
			string password )
		{
			_connectTimeout = connectTimeout;
			_conversationTimeout = conversationTimeout;
			_screenName = screenName;
			_login = login;
			_password = password;
		}

		[ReflectorProperty("connectTimeout", Required=false)]
		public Int32 ConnectTimeout
		{
			get{ return( _connectTimeout ); }
			set{ _connectTimeout = value; }
		}

		[ReflectorProperty("conversationTimeout", Required=false)]
		public Int32 ConversationTimeout
		{
			get{ return( _conversationTimeout ); }
			set{ _conversationTimeout = value; }
		}

		[ReflectorProperty("screenName", Required=false)]
		public string ScreenName
		{
			get{ return( _screenName ); }
			set{ _screenName = value; }
		}

		[ReflectorProperty("login", Required=false)]
		public string Login
		{
			get{ return( _login ); }
			set{ _login = value; }
		}

		[ReflectorProperty("password", Required=false)]
		public string Password
		{
			get{ return( _password ); }
			set{ _password = value; }
		}
	}
}