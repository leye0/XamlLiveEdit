using System;

namespace Mesharp
{
	public class MessageEnveloppe
	{
		public MessageEnveloppe (object message)
		{
			Message = message;
			TypeFullName = message.GetType().FullName;
		}

		public object Message { get; set; }

		public string TypeFullName { get; set; }
	}
}

