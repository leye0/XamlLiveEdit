using System;

namespace Mesharp
{
	public class MessageEventArgs<T> : EventArgs
	{ 
		public T Message { get; set; }
		public Guid PeerToken { get; set; }
	}

	public class MessageToHandle<T>
	{
		public delegate void MessageHandler<V, W>(V sender, W e) where V : class;

		public event MessageHandler<MessageToHandle<T>, MessageEventArgs<T>> EventHandler;

		public virtual void OnMessageEvent(MessageEventArgs<T> a)
	    {
	        EventHandler(this, a);
	    }
	}
}

