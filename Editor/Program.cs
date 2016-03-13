using System;
using Gtk;

namespace Editor
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			try
			{
				win.Show ();
			Application.Run ();
			} catch (Exception e)
			{
			var ab = e.Message;
			}

		}
	}
}
