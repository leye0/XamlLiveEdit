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

		public Messaging (string ipAddress, int port, string platform, string friendlyName)
		{
			Client = Client.Create(ipAddress, port, platform, friendlyName);
			Client.AddHandler(new ConnectWith()).Received += OnConnectWith;
			Client.AddHandler(new ReturnPeer()).Received += OnReturnPeer;
			Client.AddHandler(new Log()).Received += OnLog;
			Client.AddHandler(new BadRequest()).Received += OnBadRequest;
			Client.AddHandler(new Chocolat()).Received += OnChocolat;;

		}

		void OnChocolat (MessageToHandle<Chocolat> sender, MessageEventArgs<Chocolat> e)
		{
			
		}

		void OnBadRequest (MessageToHandle<BadRequest> sender, MessageEventArgs<BadRequest> e)
		{
		}

		void OnLog (MessageToHandle<Log> sender, MessageEventArgs<Log> e)
		{
		}

		void OnReturnPeer (MessageToHandle<ReturnPeer> sender, MessageEventArgs<ReturnPeer> e)
		{
		}

		void OnConnectWith (MessageToHandle<ConnectWith> sender, MessageEventArgs<ConnectWith> e)
		{
		}

	}
}