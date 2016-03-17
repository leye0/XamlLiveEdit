using System;
using System.Linq;
using System.Reflection;
using LiveXamlEdit.Common;
using Mesharp;
using Xamarin.Forms;
using System.Threading.Tasks;
using File = LiveXamlEdit.Messaging.File;
using System.Collections.Generic;

namespace LiveXamlEdit.Forms
{
	public class App : Application
	{
		Label _listOfIps;

		Messaging.Messaging _messaging;
		ContentPage _page;

		public App ()
		{
			var ipAddress = DependencyService.Get<IIPAddressManager> ().GetIPAddress ();
			 
			// The root page of your application
			var mePort = new Entry () { Text = "11111" };
			var textEdit = new Entry () { Text = "192.168." };
			var textEdit2 = new Entry () { Text = "11111" };
			_listOfIps = new Label {
				XAlign = TextAlignment.Center,
				Text = "",
				BackgroundColor = Color.Navy
			};
			var meButton = new Button () { Text = "Connect Me (choose port)" };
			var otherButton = new Button () { Text = "Connect Other" };
			var anotherButton = new Button () { Text = "Send a message to first peer" };

			MainPage = _page = new ContentPage {
				Content = new StackLayout {
					VerticalOptions = LayoutOptions.Center,
					Children = {
						new Label {
							XAlign = TextAlignment.Center,
							Text = ipAddress
						},
						_listOfIps,
						mePort,
						meButton,
						otherButton,
						textEdit,
						textEdit2,
						anotherButton
					}
				}
			};

			_messaging = null;

			meButton.Clicked += (object sender, EventArgs e) => {
				_messaging = new Messaging.Messaging (ipAddress, int.Parse (mePort.Text), Device.OS.ToString (), "MobileTest");
				meButton.IsEnabled = false;
			};


			otherButton.Clicked += (s, e) => {
				var ip = textEdit.Text;
				var port = int.Parse (textEdit2.Text);
				_messaging.Client.ConnectWith (new ClientInfos {
					IPAddress = ip,
					Port = port
				});
			};

			anotherButton.Clicked += (s, e) => {
				_messaging.Client.Send<Ping, Pong> (new Ping (Guid.NewGuid()), _messaging.Client.Peers.FirstOrDefault ().ClientInfos.PeerToken, Guid.NewGuid())
				.Response += (Pong obj) =>
				{
					var ab = 1;
				};
			};
		}

		protected override void OnStart () {}
		protected override void OnSleep () {}
		protected override void OnResume () {}

		private void ShowPage (string xamlContents)
		{
			try
			{
				var page = new ContentPage{ Padding = new Thickness (0, 0, 0, 0) };

				var s = (
				            ((MethodInfo)(
				                ((TypeInfo)(
				                    (
				                        typeof(Xamarin.Forms.Xaml.ArrayExtension).GetTypeInfo ().Assembly.DefinedTypes
				.Where (t => t.FullName == "Xamarin.Forms.Xaml.Extensions")
				.First ()
				                    )
				                )).DeclaredMembers
				.Where (t => !((MethodInfo)t).Attributes.HasFlag (System.Reflection.MethodAttributes.Family))
				.First ()
				            )).MakeGenericMethod (typeof(ContentPage))
				        ).Invoke (null, new object[]{ page, xamlContents }) != null;

				Xamarin.Forms.Device.BeginInvokeOnMainThread (() => {
					MainPage = page;
				});
			} catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine (e.Message);
			}
		}
	}
}
	
