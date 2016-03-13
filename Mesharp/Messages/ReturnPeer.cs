using System;
using Mesharp;

namespace Mesharp
{
	public class ReturnPeer : Message
	{
		public ReturnPeer() {}

		public ReturnPeer (Peer returnedPeer)
		{
			Content = returnedPeer;
			ContentType = typeof(Peer).FullName;
		}
	}
}
