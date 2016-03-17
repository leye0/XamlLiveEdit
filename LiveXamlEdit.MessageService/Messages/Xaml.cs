namespace LiveXamlEdit.Messaging
{
	public class Xaml
	{
		public Xaml ()
		{
		}

		public Xaml (string xamlContent)
		{
			XamlContent = xamlContent;
		}

		public string XamlContent { get; set; }
	}
}

