using System;
using Mesharp;

namespace Mesharp
{
	public class ReturnPeer
	{
		public ReturnPeer() {}

		public ReturnPeer (Peer returnedPeer, Peer[] sharedPeers)
		{
			ReturnedPeer = returnedPeer;
			SharedPeers = sharedPeers;
		}

		public Peer ReturnedPeer { get; set; }

		public Peer[] SharedPeers { get; set; }
	}
}
