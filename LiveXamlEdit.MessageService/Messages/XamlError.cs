using Mesharp;
using System;

namespace LiveXamlEdit.Messaging
{
	public class XamlError
	{
		public XamlError ()
		{
		}

		public XamlError (Exception exception)
		{
			XamlErrorMessage = exception.Message;
			XamlErrorStackTrace = exception.StackTrace;

			if (exception.InnerException != null)
			{
				XamlErrorInnerMessage = exception.InnerException.Message;
				XamlErrorInnerMessage = exception.InnerException.StackTrace;
			}
		}

		public string XamlErrorMessage { get; set; }
		public string XamlErrorStackTrace { get; set; }
		public string XamlErrorInnerMessage { get; set; }
		public string XamlErrorInnerStackTrace { get; set; }
	}
}

