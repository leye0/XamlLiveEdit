using System;

namespace Mesharp
{
	public class ConnectWith
	{
		public ConnectWith ()
		{
		}

		public ConnectWith (ClientInfos clientInfos, Peer[] sharedPeers)
		{
			ClientInfos = clientInfos;
			SharedPeers = sharedPeers;
		}

		public ClientInfos ClientInfos { get; set; }

		public Peer[] SharedPeers { get; set; }
	}
}
