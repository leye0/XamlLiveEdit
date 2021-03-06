﻿using LiveXamlEdit.Common;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LiveXamlEdit.Desktop
{
    class IPAddressManager : IIPAddressManager
    {
		public string GetIPAddress ()
		{
			var ipAddress = "";

			foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
				                netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
				{
					foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
					{
						if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							ipAddress = addrInfo.Address.ToString ();
							if (ipAddress.StartsWith ("192"))
							{
								return ipAddress;
							}
                        }
                    }
                }
            }

            return ipAddress;
        }
    }
}