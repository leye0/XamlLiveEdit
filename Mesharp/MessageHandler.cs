using System;

namespace Mesharp
{
	public class MessageEventArgs<T> : EventArgs
	{ 
		public T Message { get; set; }
		public Guid PeerToken { get; set; }
		public Guid MessageToken { get; set; }
	}

	public class BadRequest : EventArgs
	{
		public string BytesAsBase64 { get; set; }
		public ErrorReason ErrorReason { get; set; }
		public string ErrorMessage { get; set; }
		public Guid MessageToken { get; set; }
		public ClientInfos RemoteClientInfos { get; set; }
	}

	public class ErrorException : Exception
	{
		public ErrorReason Reason { get; set; }
		public ErrorException(ErrorReason reason, string message) : base(message)
		{
			Reason = reason;
		}
	}

	public enum ErrorReason
	{
		Unknown,
		OnConnection,
		OnMessageSignature,
		OnReadGuid,
		OnReadMessageToken,
		OnReadMessageLength,
		OnReadContent,
		OnDeserialize,
		PeerNotFound,
		OnHandlingMessage,
		NoHandlerForMessage
	}

	public class MessageToHandle<T>
	{
		public delegate void MessageHandler<V, W>(V sender, W e) where V : class;

		public event MessageHandler<MessageToHandle<T>, MessageEventArgs<T>> Received;

		public virtual void OnMessageEvent(MessageEventArgs<T> a)
	    {
	        Received(this, a);
	    }
	}
}

