namespace Mesharp
{
	public class BroadcastPeers
	{
		public BroadcastPeers() {}

		public BroadcastPeers (Peer[] peers)
		{
			Peers = peers;
		}

		public Peer[] Peers { get; set; }
	}
}