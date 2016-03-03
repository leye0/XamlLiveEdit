using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Sockets.Plugin;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace LiveXamlEdit
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
			InitServer();
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}

		private async void InitServer()
		{
			var listenPort = 11000;
			var listener = new TcpSocketListener();

			// when we get connections, read byte-by-byte from the socket's read stream
			listener.ConnectionReceived += async (sender, args) => 
			{
			  var client = args.SocketClient; 
			  var xamlContent = string.Empty;

			  while (true)
			  {
			    // read from the 'ReadStream' property of the socket client to receive data
			    var nextByte = await Task.Run(()=> client.ReadStream.ReadByte());

			    if (nextByte == -1)
			    {
			    	break;
			    }

			    xamlContent += (char)nextByte;

			    if (xamlContent.StartsWith("1234567890"))
			    {
					xamlContent = string.Empty;
			    }

				if (xamlContent.EndsWith("0987654321"))
			    {
					xamlContent = xamlContent.Replace("0987654321", "");

					if (xamlContent.Contains("<?xml"))
					{
						ShowPage(xamlContent);
						xamlContent = string.Empty;
					}
			    }
			  }
			};

			// bind to the listen port across all interfaces
			await listener.StartListeningAsync(listenPort);
		}

		private void ShowPage (string xamlContents)
		{
			try
			{
				var page = new ContentPage{Padding = new Thickness(0,0,0,0)};

				var s = (
				((MethodInfo)(
				((TypeInfo)(
				(
				typeof(Xamarin.Forms.Xaml.ArrayExtension).GetTypeInfo().Assembly.DefinedTypes
				.Where(t => t.FullName == "Xamarin.Forms.Xaml.Extensions")
				.First()
				)
				)).DeclaredMembers
				.Where(t => !((MethodInfo)t).Attributes.HasFlag(System.Reflection.MethodAttributes.Family))
				.First()
				)).MakeGenericMethod(typeof(ContentPage))
				).Invoke(null, new object[]{page, xamlContents}) != null;

				Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
				{
					MainPage = page;
				});
			} catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e.Message);
			}
		}
	}
}
	
