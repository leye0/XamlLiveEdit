﻿namespace Mesharp
{
	public class Message
	{
		public Message() {}

		public Message (string text)
		{
			Text = text;
		}

		public string Text { get; set; }
	}
}