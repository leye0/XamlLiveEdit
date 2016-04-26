﻿using Gtk;
using System;
using System.Linq;
using LiveXamlEdit.Desktop;
using LiveXamlEdit.Messaging;
using System.Collections.Generic;
using Mesharp;
using System.Threading.Tasks;

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
			if (textview2.Buffer != null && !string.IsNullOrWhiteSpace(textview2.Buffer.Text))
			{
				RefreshXaml(textview2.Buffer.Text);
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

	private Messaging _messaging;

	private async Task InitClient ()
	{
		var ipAddress = new IPAddressManager().GetIPAddress();
		_messaging = new Messaging(ipAddress, 11112, "Desktop", "DesktopTest");
		_messaging.Client.AddHandler(new XamlError()).Received += XamlError;

		//_messaging.Client.AddHandler(new ConnectWith()).Received += OnConnect;

//		var content = (await _messaging.Client.Send<ConnectWith, ReturnPeer>(new ConnectWith(_messaging.Client.ClientInfos, _messaging.Client.Peers.ToArray()), Guid.NewGuid(), new ClientInfos {
//			IPAddress = "192.168.12.227",
//			Port = 11112
//		})).ResponseContent;
//
	}

	void OnConnect (Mesharp.MessageToHandle<ConnectWith> sender, Mesharp.MessageEventArgs<ConnectWith> e)
	{
		
	}

	void XamlError (Mesharp.MessageToHandle<XamlError> sender, Mesharp.MessageEventArgs<XamlError> e)
	{
	}

	private void RefreshXaml(string xamlContent)
	{
		 var ab = _messaging.Client.Send(new Xaml(xamlContent), _messaging.Client.Peers.FirstOrDefault(x => x.ClientInfos.Platform == "Android")
		.ClientInfos.PeerToken, Guid.Empty);
	}
}
