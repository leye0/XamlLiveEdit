namespace Mesharp
{
	public class Pong
	{
		public Pong() {}

		public Pong (System.Guid pingId)
		{
			PingId = pingId;
		}

		public System.Guid PingId { get; set; }
	}
}