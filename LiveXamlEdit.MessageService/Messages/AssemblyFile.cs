namespace LiveXamlEdit.Messaging
{
	public class AssemblyFile
	{
		public AssemblyFile ()
		{
		}

		public AssemblyFile (string name, byte[] data)
		{
			Name = name;
			Data = data;
		}

		public string Name { get; set; }

		public byte[] Data { get; set; }
	}
}

