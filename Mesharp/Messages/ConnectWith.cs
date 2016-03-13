using System;

namespace Mesharp
{
	public class ConnectWith : Message
	{
		public ConnectWith ()
		{
		}

		public ConnectWith (ClientInfos clientInfos)
		{
			Content = clientInfos;
			ContentType = typeof(ClientInfos).FullName;
		}
	}
}
