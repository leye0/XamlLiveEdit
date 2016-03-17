using System;
using System.Linq;
using System.Reflection;
using LiveXamlEdit.Common;
using Mesharp;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using LiveXamlEdit.Messaging;

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
			_messaging.Client.ConnectWith(new ClientInfos
			{
				IPAddress = "192.168.12.165",
				Port = 11006
			});
		}

		void OnXamlReceived (MessageToHandle<Xaml> sender, MessageEventArgs<Xaml> e)
		{
			try
			{
				ShowPage(e.Message.XamlContent);
			} 
			catch (Exception exception)
			{
				_messaging.Client.Send(new XamlError(exception), e.PeerToken, null);
			}
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
	
