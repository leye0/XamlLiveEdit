using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using Sockets.Plugin;
using Mesharp;
using LiveXamlEdit.Messaging;

namespace LiveXamlEdit.Forms
{
	public class App : Application
	{
		public App ()
		{
			// The root page of your application
			MainPage = new ContentPage {
				Content = new StackLayout {
					VerticalOptions = LayoutOptions.Center,
					Children = {
						new Label {
							XAlign = TextAlignment.Center,
							Text = "Welcome to Xamarin Forms!"
						}
					}
				}
			};

			var ipAddress = DependencyService.Get<IIPAddressManager>().GetIPAddress();
			var messaging = new Messaging.Messaging(ipAddress, 11001, Device.OS.ToString());

			messaging.ConnectWith(new ClientInfos
			{
				IPAddress = "192.168.2.17",
				Port = 11006
			});
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
	
