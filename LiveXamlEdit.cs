using System;
using System.Linq;
using System.Reflection;
using LiveXamlEdit.Common;
using Mesharp;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using LiveXamlEdit.Messaging;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Http;
using PCLStorage;
using System.Xml.Linq;

namespace LiveXamlEdit.Forms
{
	public class App : Application
	{
		Messaging.Messaging _messaging;
		ContentPage _page;

		public App ()
		{
			//InitPlaygroundUi();

			InitRenderingTestUi();
		}

		string ip;
		void InitRenderingTestUi ()
		{
			ip = DependencyService.Get<IIPAddressManager> ().GetIPAddress ();
			_messaging = new Messaging.Messaging (ip, 11111, Device.OS.ToString (), "MobileTest");

			MainPage = _page = new ContentPage {
				Content = new StackLayout {
					VerticalOptions = LayoutOptions.Center,
					Children = {
						new Label {
							XAlign = TextAlignment.Center,
							Text = ip
						},
					}
				}
			};

			_messaging.Client.AddHandler(new Xaml()).Received += OnXamlReceived;
//			_messaging.Client.ConnectWith(new ClientInfos
//			{
//				IPAddress = "192.168.12.165",
//				Port = 11111
//			});


			LoadAssemblyPOC();
		}

		// Downloads a .dll and loads types.
		// It seems like this security issue in XS has been resolved Q1 2016. :(
		private async Task LoadAssemblyPOC ()
		{
			var client = new HttpClient ();
			var assembly = await client.GetAsync ("https://drive.google.com/uc?export=download&id=0B2CLiIAK3uPmdUZxXzlXUm5WMm8");
			var assemblyBytes = await assembly.Content.ReadAsByteArrayAsync ();
			var fs = PCLStorage.FileSystem.Current;
			var fold = await fs.GetFolderFromPathAsync ("/data/data/com.livexamledit/files/.__override__"); // cache files/.__config__
			var file = await fold.CreateFileAsync("DummyPresentation.DummyViewModels.dll", CreationCollisionOption.ReplaceExisting);
			file.WriteAllBytes(assemblyBytes);
			var test = await fold.GetFilesAsync();
			OuterAssembly = Assembly.Load(new AssemblyName("DummyPresentation.DummyViewModels.dll"));
		}

		void OnXamlReceived (MessageToHandle<Xaml> sender, MessageEventArgs<Xaml> e)
		{
			ShowPage (e.Message.XamlContent, e.PeerToken);
		}

		void InitPlaygroundUi()
		{
			var ipAddress = DependencyService.Get<IIPAddressManager> ().GetIPAddress ();

			var mePort = new Entry () { Text = "11111", HorizontalOptions = LayoutOptions.Fill };
			var meButton = new Button () { Text = "Connect Me (choose port)", HorizontalOptions = LayoutOptions.Fill };

			var otherIp = new Entry () { Text = "192.168.12.", HorizontalOptions = LayoutOptions.Fill };
			var otherPort = new Entry () { Text = "11111", HorizontalOptions = LayoutOptions.Fill };
			var otherConnect = new Button () { Text = "Connect Other", HorizontalOptions = LayoutOptions.Fill };

			var sendMessageToFirst = new Button () { Text = "Send a message to first peer" };

			var logClear = new Button() { Text = "Clear" };
			var logScroll = new ScrollView { Orientation = ScrollOrientation.Vertical, VerticalOptions = LayoutOptions.FillAndExpand };
			var logView = new Label()
			{ 	
				Text = "",
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.StartAndExpand,
				HeightRequest = 1000,
				LineBreakMode = LineBreakMode.CharacterWrap,
				FontSize = 8
		  	};

		  	logScroll.Content = logView;

			var stack1 = new StackLayout{Orientation = StackOrientation.Horizontal};
			stack1.Children.Add(mePort);
			stack1.Children.Add(meButton);

			var stack2 = new StackLayout{Orientation = StackOrientation.Horizontal};
			stack2.Children.Add(otherIp);
			stack2.Children.Add(otherPort);
			stack2.Children.Add(otherConnect);

			var stack3 = new StackLayout{Orientation = StackOrientation.Horizontal};
			stack3.Children.Add(sendMessageToFirst);
			stack3.Children.Add(logClear);

			MainPage = _page = new ContentPage {
				Content = new StackLayout {
					VerticalOptions = LayoutOptions.Center,
					Children = {
						new Label {
							XAlign = TextAlignment.Center,
							Text = ipAddress
						},
						stack1,
						stack2,
						stack3,
						logScroll
					}
				}
			};

			_messaging = null;

			meButton.Clicked += (object sender, EventArgs e) => {
				_messaging = new Messaging.Messaging (ipAddress, int.Parse (mePort.Text), Device.OS.ToString (), "MobileTest");
				meButton.IsEnabled = false;
				_messaging.LogReceived += (MessageEventArgs<Log> obj) => 
				{
					Xamarin.Forms.Device.BeginInvokeOnMainThread (() => {
						logView.Text += obj.Message.Text;
					});
				};
			};

			otherConnect.Clicked += (s, e) => {
				var ip = otherIp.Text;
				var port = int.Parse (otherPort.Text);
				_messaging.Client.ConnectWith (new ClientInfos {
					IPAddress = ip,
					Port = port
				});
			};

			logClear.Clicked += (object sender, EventArgs e) => 
			{
				logView.Text = "";
			};
		}

		protected override void OnStart () {}
		protected override void OnSleep () {}
		protected override void OnResume () {}

		public Assembly OuterAssembly { get; set; } 

		// Wrong peer token (dest), need src.
		private async void ShowPage (string xamlContents, Guid peerToken)
		{

			// Check content before
			try
			{
				var xmlDoc = XDocument.Parse (xamlContents);

			} catch (Exception e)
			{
				_messaging.Client.Send (new XamlError (e), peerToken, null);
			}

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
				_messaging.Client.Send (new XamlError (e), peerToken, null);
			}
		}
	}
}
	
