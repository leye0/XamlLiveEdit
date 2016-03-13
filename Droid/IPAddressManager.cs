using System.Net;
using Xamarin.Forms;
using Mesharp;[assembly: Dependency(typeof(LiveXamlEdit.Forms.Droid.IPAddressManager))]

namespace LiveXamlEdit.Forms.Droid
{
    class IPAddressManager : IIPAddressManager
    {
        public string GetIPAddress()
        {
            IPAddress[] adresses = Dns.GetHostAddresses(Dns.GetHostName());

            if (adresses !=null && adresses[0] != null)
            {
                return adresses[0].ToString();
            }
            else
            {
                return null;
            }
        }
    }
}