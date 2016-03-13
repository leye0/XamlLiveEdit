using System;
using Mesharp;
using System.Threading.Tasks;
using PCLStorage;
using System.IO;

namespace LiveXamlEdit.Messaging
{
	// This is where the shared logic for messaging goes.
	// It does the same thing than the client but business logic should be integrated in this project.

	public class Messaging
	{
		public Client Client { get; set; }

		public Messaging (string ipAddress, int port, string platform)
		{
			Client = Client.Create(ipAddress, port, platform);
			Client.RegisterMessageEventHandler(new SynchronizeFile()).EventHandler += OnSynchronizeFile;
//			SynchronizeFile();
		}

//		public async Task SynchronizeFile()
//		{
//		}

		private void OnSynchronizeFile (MessageToHandle<SynchronizeFile> sender, MessageEventArgs<SynchronizeFile> e)
		{
			var fileData = e.Message;
			var file = (File) fileData.Content;
		}

		public async Task ConnectWith (ClientInfos destinationClientInfos)
		{
			var originClientInfo = Client.ClientInfos;
			await Client.Send(new ConnectWith(originClientInfo), Guid.NewGuid(), destinationClientInfos);
		}
	}
}

