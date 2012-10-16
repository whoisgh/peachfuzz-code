﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace Peach.Core.Publishers
{
	[Publisher("Udp", true)]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host", true)]
	[Parameter("Port", typeof(ushort), "Destination port number", true)]
	[Parameter("Timeout", typeof(int), "How many seconds to wait for data/connection (default 3)", "3")]
	[Parameter("SrcPort", typeof(ushort), "Source port number", "0")]
	public class UdpPublisher : SocketPublisher
	{
		public UdpPublisher(Dictionary<string, Variant> args)
			: base("Udp", args)
		{
		}

		protected override Socket OpenSocket()
		{
			IPAddress remote = Dns.GetHostAddresses(Host)[0];
			Socket s = new Socket(remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			if (remote.AddressFamily == AddressFamily.InterNetwork)
				s.Bind(new IPEndPoint(IPAddress.Any, SrcPort));
			else
				s.Bind(new IPEndPoint(IPAddress.IPv6Any, SrcPort));
			s.Connect(remote, Port);
			return s;
		}
	}
}
