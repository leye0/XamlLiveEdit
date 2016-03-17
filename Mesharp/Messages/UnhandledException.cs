using System;

namespace Mesharp
{
	public class UnhandledException
	{
		public UnhandledException() {}

		public UnhandledException (Exception exception)
		{
			Exception = exception.Message + ";" + exception.StackTrace.ToString();
		}

		public string Exception { get; set; }
	}
}