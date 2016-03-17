using Mesharp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LiveXamlEdit.Messaging
{
	// This is where the shared logic for messaging goes.
	// It does the same thing than the client but business logic should be integrated in this project.

	public class Messaging
	{
		public Client Client { get; set; }

		public event Action<MessageEventArgs<Log>> LogReceived;

		public Messaging (string ipAddress, int port, string platform, string friendlyName)
		{
			Client = Client.Create(ipAddress, port, platform, friendlyName, this);

			Client.AddHandler(new Log()).Received += OnLog;
		}

		void OnLog (MessageToHandle<Log> sender, MessageEventArgs<Log> e)
		{
			LogReceived.Invoke(e);
		}
	}
}