using Mesharp;
using System;
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
			Client.AddHandler(new ConnectWith()).EventHandler += OnConnectWith;
			Client.AddHandler(new ReturnPeer()).EventHandler += OnReturnPeer;

		}

		void OnReturnPeer (MessageToHandle<ReturnPeer> sender, MessageEventArgs<ReturnPeer> e)
		{
			
		}

		void OnConnectWith (MessageToHandle<Mesharp.ConnectWith> sender, MessageEventArgs<Mesharp.ConnectWith> e)
		{
		}

		public async Task ConnectWith (ClientInfos destinationClientInfos)
		{
			var originClientInfo = Client.ClientInfos;
			await Client.Send(new ConnectWith(originClientInfo, Client.Peers.ToArray()), Guid.NewGuid(), destinationClientInfos);
		}
	}
}