using System;
using Gtk;
using System.Threading.Tasks;
using Sockets.Plugin;
using Mesharp;
using System.Collections.Generic;
using LiveXamlEdit.Messaging;
using LiveXamlEdit.Desktop;

public partial class MainWindow: Gtk.Window
{
	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Build ();
		Init();
		InitClient();
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

	private void InitClient ()
	{
		var ipAddress = new IPAddressManager().GetIPAddress();
		var messaging = new Messaging(ipAddress, 11006, "Desktop");
	}

	private void RefreshXaml()
	{
//		SendData("1234567890" + textview2.Buffer.Text + "0987654321");
	}
}
