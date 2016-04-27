using System;
using System.IO;
using System.Collections.Generic;
using Mesharp;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using LiveXamlEdit.Messaging;
using LiveXamlEdit.Server;

namespace LiveXamlEdit.Server
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Xaml Live Edit");
			new App(args);
			Application.Run();
		}

		public class App
		{
			private Dictionary<string, string> Assemblies = new Dictionary<string, string>();
			private Dictionary<string, string> Xamls = new Dictionary<string, string>();

			private Mesharp.Client _mesh;

			public App(string[] args) 
			{
				var hcPath = "/Projects/Vingo/Src/Mobile/Vingo.Views";
				Connect();
				AddHandlers();
				AddAssemblies(hcPath);
				WatchXamls(hcPath);
			}

			private void Connect()
			{
				var ipManager = new IPAddressManager();
				var ip = ipManager.GetIPAddress();
				_mesh = Client.Create(ip, 11112, "Desktop", "Server", this);
				Console.WriteLine("Server Connected on " + ip + ":11112");
			}

			private void AddHandlers ()
			{
				// if a device is connected:
				_mesh.AddHandler(new ConnectWith()).Received += DeviceConnected;
				_mesh.AddHandler(new ReturnPeer()).Received += PeerReceived;
			}

			void PeerReceived (MessageToHandle<ReturnPeer> sender, MessageEventArgs<ReturnPeer> e)
			{
				Console.WriteLine("Peer received:");
				Console.WriteLine(JsonConvert.SerializeObject(e.Message, Formatting.Indented));
				Console.WriteLine("");
			}

			private void DeviceConnected (MessageToHandle<ConnectWith> sender, MessageEventArgs<ConnectWith> e)
			{
				// Debug:
				Console.WriteLine("Device connected:");
				Console.WriteLine(JsonConvert.SerializeObject(e.Message, Formatting.Indented));
				Console.WriteLine("");
				SyncDevice(e.PeerToken);



			}

			private void SyncDevice (Guid peerToken)
			{
				var device = _mesh.Peers.FirstOrDefault (x => x.ClientInfos.PeerToken == peerToken);
				if (device == null) return;

				// We want to share the current state of the project with the device
				var assemblyNameTest = AssembliesNeeded[0];
				var assemblyDataTest = File.ReadAllBytes(Assemblies[assemblyNameTest]);

				// Share the assemblies
				_mesh.Send(new AssemblyFile() {Name = assemblyNameTest, Data = assemblyDataTest}, device.ClientInfos.PeerToken);

				// Share the XAML
			}

			private void AddAssemblies (string path)
			{
				Assemblies = ProcessDirectory (path, Assemblies, "*.dll");
				Xamls = ProcessDirectory (path, Xamls, "*.xaml");
				AssembliesNeeded = new List<string> ();

				foreach (var file in Xamls.Select(x => x.Value))
				{
					var xmlDoc = XDocument.Parse (File.ReadAllText (file));
					var detectedAssemblies = xmlDoc.Root.Attributes ().Where (x => x.Name.NamespaceName.Contains ("xmlns") && x.Value.Contains ("assembly="))
						.Select (x => x.Value.ToString ().Substring (x.Value.ToString ().IndexOf ("assembly=") + "assembly=".Length));

					AssembliesNeeded.AddRange (detectedAssemblies.Where (newAssembly => !AssembliesNeeded.Contains (newAssembly)));

					var referencesToAdd = new List<string>();
					foreach (var neededAssembly in AssembliesNeeded)
					{
						if (AssemblyReferences.ContainsKey(neededAssembly))
						{
							var refs = AssemblyReferences[neededAssembly];
							referencesToAdd.AddRange (refs.Where (newAssembly => !referencesToAdd.Concat(AssembliesNeeded).Contains (newAssembly)));
						}
					}
					AssembliesNeeded.AddRange(referencesToAdd);
				}
			}

			public List<string> AssembliesNeeded { get; set; }

			// Process all files in the directory passed in, recurse on any directories 
		    // that are found, and process the files they contain.
			public Dictionary<string, string> ProcessDirectory (string targetDirectory, Dictionary<string, string> files, string filter)
			{
				// Process the list of files found in the directory.
				var fileEntries = Directory.GetFiles (targetDirectory, filter);
				foreach (string fileName in fileEntries)
				{
					string name;

					if (filter == "*.dll")
					{
						var assembly = System.Reflection.Assembly.UnsafeLoadFrom (fileName);
						var refs = assembly.GetReferencedAssemblies();
						name = assembly.ManifestModule.Name.Replace(".dll", "");
						AssemblyReferences[name] = refs.Select(x => x.Name).ToArray();
					} else
					{
						name = Path.GetFileNameWithoutExtension(fileName);
					}

					if (!files.ContainsKey (name))
					{
						files.Add(name, fileName);
					}

				}

				// Recurse into subdirectories of this directory.
				var subdirectoryEntries = Directory.GetDirectories (targetDirectory);

				foreach (string subdirectory in subdirectoryEntries)
				{
					files = ProcessDirectory(subdirectory, files, filter);
				}

				return files;
		    }

		    Dictionary<string, string[]> AssemblyReferences = new Dictionary<string, string[]>();

		    private void WatchXamls(string path)
		    {
				// Create a new FileSystemWatcher and set its properties.
		        var watcher = new FileSystemWatcher();
				watcher.IncludeSubdirectories = true;
		        watcher.Path = path;
		        /* Watch for changes in LastAccess and LastWrite times, and
		           the renaming of files or directories. */
		        watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
		           | NotifyFilters.FileName | NotifyFilters.DirectoryName;
		        // Only watch text files.
		        watcher.Filter = "*.xaml";

		        // Add event handlers.
		        watcher.Changed += new FileSystemEventHandler(OnChanged);
		        watcher.Created += new FileSystemEventHandler(OnChanged);
		        watcher.Deleted += new FileSystemEventHandler(OnChanged);
		        watcher.Renamed += new RenamedEventHandler(OnRenamed);

		        // Begin watching.
		        watcher.EnableRaisingEvents = true;
		    }

			// Define the event handlers.
			// TODO: Bug: Wrong events are fired
			private void OnChanged (object source, FileSystemEventArgs e)
			{
				Console.WriteLine(Xamls.FirstOrDefault(x => x.Value == e.FullPath).Key + " changed!");
		    }

		    private void OnRenamed(object source, RenamedEventArgs e)
		    {
		        // Specify what is done when a file is renamed.
		        Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
		    }
		}

	    private static void ShowQuit ()
		{
			Console.WriteLine ("Press \'q\' to quit the sample.");
			while (Console.Read () != 'q');
		}
	}
}
