using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Analyzers;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace Peach.Core.Test.Publishers
{
	class SimpleTcpClient
	{
		private EndPoint localEP;
		private Socket Socket;
		private bool Graceful;
		public string Result = null;


		public SimpleTcpClient(ushort port, bool graceful)
		{
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			localEP = new IPEndPoint(IPAddress.Loopback, port);
			Graceful = graceful;
		}

		public void Start()
		{
			Socket.BeginConnect(localEP, OnConnect, null);
		}

		public void OnConnect(IAsyncResult ar)
		{
			try
			{
				Socket.EndConnect(ar);

				Socket.Send(Encoding.ASCII.GetBytes("Test buffer"));
				byte[] recv = new byte[1024];
				int len = Socket.Receive(recv);
				Result = Encoding.ASCII.GetString(recv, 0, len);
				if (Graceful)
				{
					Socket.Shutdown(SocketShutdown.Both);
				}
				else
				{
					do
					{
						len = Socket.Receive(recv);
					}
					while (len > 0);
				}
				Socket.Close();
				Socket = null;
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.ConnectionRefused)
				{
					System.Threading.Thread.Sleep(250);
					Start();
				}
				else
				{
					throw;
				}
			}
		}
	}


	class SimpleTcpServer
	{
		private EndPoint localEP;
		private Socket Socket;
		private bool Graceful;
		public string Result = null;


		public SimpleTcpServer(ushort port, bool graceful)
		{
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			localEP = new IPEndPoint(IPAddress.Loopback, port);
			Graceful = graceful;
			Socket.Bind(localEP);
			Socket.Listen(8);
		}

		public void Start()
		{
			Socket.BeginAccept(OnAccept, null);
		}

		public void OnAccept(IAsyncResult ar)
		{
			try
			{
				Socket cli = Socket.EndAccept(ar);

				cli.Send(Encoding.ASCII.GetBytes("Test buffer"));
				byte[] recv = new byte[1024];
				int len = cli.Receive(recv);
				Result = Encoding.ASCII.GetString(recv, 0, len);
				if (Graceful)
				{
					cli.Shutdown(SocketShutdown.Both);
				}
				else
				{
					do
					{
						len = cli.Receive(recv);
					}
					while (len > 0);
				}
				cli.Close();
				cli = null;
				Socket.Close();
				Socket = null;
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.ConnectionRefused)
				{
					System.Threading.Thread.Sleep(250);
					Start();
				}
				else
				{
					throw;
				}
			}
		}
	}


	[TestFixture]
	class TcpPublisherTests : DataModelCollector
	{
		public string template = @"
<Peach>

	<DataModel name=""TheDataModel"">
		<String name=""str"" value=""Hello World""/>
	</DataModel>

	<DataModel name=""TheDataModel2"">
		<String name=""str"" value=""Hello World""/>
	</DataModel>

	<StateModel name=""ListenState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Accept"" type=""accept""/>

			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>

			<Action name=""Send"" type=""output"">
				<DataModel ref=""TheDataModel2""/>
			</Action>
		</State>
	</StateModel>

	<StateModel name=""ClientState"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Recv"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>

			<Action name=""Send"" type=""output"">
				<DataModel ref=""TheDataModel2""/>
			</Action>
		</State>
	</StateModel>

<Test name=""Default"">
		<StateModel ref=""{0}""/>
		<Publisher class=""{1}"">
			<Param name=""{2}"" value=""127.0.0.1""/>
			<Param name=""Port"" value=""{3}""/>
		</Publisher>
	</Test>

</Peach>
";
		public void TcpServer(bool clientShutdown)
		{
			ushort port = (ushort)((Environment.TickCount % 10000) + 20000);
			SimpleTcpClient cli = new SimpleTcpClient(port, clientShutdown);
			cli.Start();

			string xml = string.Format(template, "ListenState", "TcpListener", "Interface", port);

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.config = config;
			e.startFuzzing(dom, config);

			Assert.AreEqual(2, actions.Count);

			var de1 = actions[0].dataModel.find("TheDataModel.str");
			Assert.NotNull(de1);
			var de2 = actions[1].dataModel.find("TheDataModel2.str");
			Assert.NotNull(de2);

			string send = (string)de2.DefaultValue;
			string recv = (string)de1.DefaultValue;

			Assert.AreEqual("Hello World", send);
			Assert.AreEqual("Test buffer", recv);

			Assert.NotNull(cli.Result);
			Assert.AreEqual("Hello World", cli.Result);
		}

		[Test]
		public void TcpListenShutdownClient()
		{
			// Test TcpListener deals with client initiating shutdown
			TcpServer(true);
		}

		[Test]
		public void TcpListenShutdownServer()
		{
			// Test TcpListener deals with it initiating shutdown
			TcpServer(false);
		}

		public void TcpClient(bool serverShutdown)
		{
			ushort port = (ushort)((Environment.TickCount % 10000) + 20000);
			SimpleTcpServer cli = new SimpleTcpServer(port, serverShutdown);
			cli.Start();

			string xml = string.Format(template, "ClientState", "TcpClient", "Host", port);

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.config = config;
			e.startFuzzing(dom, config);

			Assert.AreEqual(2, actions.Count);

			var de1 = actions[0].dataModel.find("TheDataModel.str");
			Assert.NotNull(de1);
			var de2 = actions[1].dataModel.find("TheDataModel2.str");
			Assert.NotNull(de2);

			string send = (string)de2.DefaultValue;
			string recv = (string)de1.DefaultValue;

			Assert.AreEqual("Hello World", send);
			Assert.AreEqual("Test buffer", recv);

			Assert.NotNull(cli.Result);
			Assert.AreEqual("Hello World", cli.Result);
		}

		[Test]
		public void TcpClientShutdownClient()
		{
			// Test TcpClient deals with itself initiating shutdown
			TcpClient(false);
		}

		[Test]
		public void TcpClientShutdownServer()
		{
			// Test TcpListener deals with client initiating shutdown
			TcpClient(true);
		}
	}
}
