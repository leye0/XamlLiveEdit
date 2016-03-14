namespace Mesharp
{
	public class Ping
	{
		public Ping() {}

		public Ping (System.Guid pingId)
		{
			PingId = pingId;
		}

		public System.Guid PingId { get; set; }
	}
}