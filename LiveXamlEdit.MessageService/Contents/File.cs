using System;

namespace LiveXamlEdit.Messaging
{
	public class File
	{
		public File ()
		{
		}

		public Guid ProjectId { get; set; }

		public string Project { get; set; }

		public Guid FileId { get; set; }

		public string Filename { get; set; }

		public string DataBase64 { get; set; }
	}
}

