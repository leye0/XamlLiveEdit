using System;
using Gtk;
using System.Threading.Tasks;
using Sockets.Plugin;

public partial class MainWindow: Gtk.Window
{
	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Build ();
		Init();
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	private void Init()
	{
		OpenAction1.Activated += (object sender, EventArgs e) => 
		{
			OpenOFD();
		};

		InitClient();
		textview2.Editable = true;
		textview2.Buffer.Changed += (o, args) => 
		{
			if (!string.IsNullOrWhiteSpace(textview2.Buffer.Text))
			{
				RefreshXaml();
			}
		};
	}

	private void OpenOFD()
	{
	    Gtk.FileChooserDialog filechooser =
	        new Gtk.FileChooserDialog("Select a XAML view",
	            this,
	            FileChooserAction.Open,
	            "Cancel",ResponseType.Cancel,
	            "Open",ResponseType.Accept);

	    filechooser.AddFilter(new FileFilter());
        filechooser.Filter.AddPattern("*.xaml");

	    if (filechooser.Run() == (int)ResponseType.Accept) 
	    {
			textview2.Buffer.Text = System.IO.File.ReadAllText(filechooser.Filename);
	    }	

	    filechooser.Destroy();
	}

	TcpSocketClient _client;

	private async void InitClient ()
	{
//		var address = "192.168.2.13";
		var address = "127.0.0.1";
		var port = 11000;
		_client = new TcpSocketClient ();
		await _client.ConnectAsync (address, port);
	}

	private async void SendData(string data)
	{
		var bytes = System.Text.Encoding.UTF8.GetBytes(data);
		_client.WriteStream.Write (bytes, 0, bytes.Length);
		await _client.WriteStream.FlushAsync ();
	}

	private async void DisconnectClient()
	{
		await _client.DisconnectAsync ();
	}

	private void RefreshXaml()
	{
		SendData("1234567890" + textview2.Buffer.Text + "0987654321");
	}
}
