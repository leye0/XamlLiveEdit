using Mesharp;
using System;

namespace Mesharp
{
	public class Message : IMessage
	{
		public Message ()
		{
			Kind = this.GetType().FullName.ToString();
		}

		public string Kind { get; set; }

		public object Content { get; set; }

		public string ContentType { get; set; }

		public string AsJson()
		{
			return SimpleJson.SerializeObject(new { this.Kind, this.Content, this.ContentType });
		}
	}

	public interface IMessage
	{
		string Kind { get; set; }
		object Content { get; set; }
		string ContentType { get; set; }
	}
}