using System;
using System.Collections.Generic;

namespace LiveXamlEdit.Messaging
{
	public class File
	{
		public File ()
		{
		}

		public File (byte[] data)
		{
			Properties = new Dictionary<string, string>();
			DataBase64 = Convert.ToBase64String(data);
		}

		public File (byte[] data, Dictionary<string, string> properties)
		{
			Properties = properties;
			DataBase64 = Convert.ToBase64String(data);
		}

		// Since usage can/will vary a lot, we keep it an open object by using a dictionary
		public Dictionary<string, string> Properties { get; set; } 

		public string DataBase64 { get; set; }

		public byte[] Data()
		{
			return Convert.FromBase64String(DataBase64);
		}
	}
}

