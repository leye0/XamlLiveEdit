using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sockets.Plugin;
using Sockets.Plugin.Abstractions;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mesharp
{
	public class Client
	{
		public static Client Create (string ipAddress, int port, string platform, string friendlyName)
		{
			return new Client(ipAddress, port, platform, friendlyName);
		}

		public static byte[] MessageSignature = new byte[16] { 110, 108, 89, 202, 220, 203, 79, 76, 156, 153, 160, 132, 85, 194, 39, 233 };

		public static byte[] BroadcastSignature = new byte[16] { 76, 246, 208, 105, 232, 113, 153, 72, 183, 13, 138, 24, 124, 236, 199, 254 };

		public List<Peer> Peers = new List<Peer>();

		public List<Guid> BroadcastMessageHistory = new List<Guid>();

		public ClientInfos ClientInfos { get; set; }

		public Client (string ipAddress, int port, string platform, string friendlyName)
		{
			ClientInfos = new ClientInfos()
			{
				IPAddress = ipAddress,
				Port = port,
				Platform = platform,
				PeerToken = Guid.NewGuid()
			};

			AddHandler(new ConnectWith()).EventHandler += OnConnectWith;
			AddHandler(new ReturnPeer()).EventHandler += OnReturnPeer;
			AddHandler(new Ping()).EventHandler += OnPing;
			AddHandler(new Pong()).EventHandler += OnPong;
			AddHandler(new BroadcastPeers()).EventHandler += OnBroadcastPeers;

			Init();
		}

		private async Task Init ()
		{
			var listenPort = ClientInfos.Port;

			var listener = new TcpSocketListener ();

			listener.ConnectionReceived += async (sender, args) => {

				using (var client = args.SocketClient)
				{
					if (!client.ReadStream.CanRead)
					{
						await client.DisconnectAsync();
					}

					try 
					{
						var guidBuffer = new byte[16];
						var lenBuffer = new byte[4];
						var isBroadcast = false;
						var isValid = false;
						var peerToken = Guid.Empty;
						var messageToken = Guid.Empty;

						guidBuffer = await ReadOrTimeout(client, guidBuffer.Length, TimeSpan.FromSeconds(2)).ConfigureAwait(true);

						if (DataMatch(guidBuffer, MessageSignature))
						{
							isValid = true;
						}

						if (DataMatch(guidBuffer, BroadcastSignature))
						{
							isValid = true;
							isBroadcast = true;
						}

						if (!isValid)
						{
							return;
						}

						guidBuffer = await ReadOrTimeout(client, guidBuffer.Length, TimeSpan.FromSeconds(2)).ConfigureAwait(false);
	
						messageToken = new Guid(guidBuffer);

						if (Broadcasted.Contains(messageToken))
						{
							await client.DisconnectAsync();
							return;
						}

						guidBuffer = await ReadOrTimeout(client, guidBuffer.Length, TimeSpan.FromSeconds(2)).ConfigureAwait(false);
						peerToken = new Guid(guidBuffer);

						lenBuffer = await ReadOrTimeout(client, lenBuffer.Length, TimeSpan.FromSeconds(2)).ConfigureAwait(false);
						if (BitConverter.IsLittleEndian)
						{
							Array.Reverse(lenBuffer);
						}
						var len = BitConverter.ToInt32(lenBuffer, 0);
						var contentBuffer = new byte[len];
						contentBuffer = await ReadOrTimeout(client, contentBuffer.Length, TimeSpan.FromSeconds(2));

						var json = System.Text.Encoding.UTF8.GetString(contentBuffer, 0, contentBuffer.Length);

						var deserialized = JsonConvert.DeserializeObject(json) as JObject;

						if (deserialized != null)
						{
							var typeFullName = (deserialized.GetValue("TypeFullName") as JToken).ToString();
							var innerMessage = (deserialized.GetValue("Message") as JToken).ToString();
							var typedMessage = JsonConvert.DeserializeObject(innerMessage, FindType(typeFullName));

							// If the peertoken is not part of the list and non-empty, and we are not broadcasting, it is totally irrelevant for us.
							if (peerToken != Guid.Empty && !Peers.Any(x => x.Token == peerToken) && !isBroadcast && typeFullName != typeof(ReturnPeer).FullName)
							{
								return;
							}

							HandleMessage(typedMessage, peerToken, new Guid?(messageToken), isBroadcast);
							HandleMessage(new Log(deserialized.ToString()), peerToken, messageToken, false);
						}

					} 
					catch (Exception e)
					{
						var peer = Peers.FirstOrDefault(x => client.RemoteAddress.ToString().ToLower().Contains(x.ClientInfos.IPAddress.ToLower()));
						if (peer != null)
						{
							HandleMessage(new Log("Error: " + e.Message), peer.Token, Guid.Empty, false);
						}
					}
					finally
					{
						await client.DisconnectAsync();
					}
				}
			};

			await listener.StartListeningAsync (listenPort);
		}

		// TODO: Older clientInfos could override newer clientInfos. Priority should be based on a date
		void OnBroadcastPeers (MessageToHandle<BroadcastPeers> sender, MessageEventArgs<BroadcastPeers> e)
		{
			var peersList = e.Message.Peers.ToList();

			foreach (var newPeer in peersList)
			{
				if (Peers.Any (x => x.ClientInfos.IPAddress == newPeer.ClientInfos.IPAddress))
				{
					Peers.RemoveAll(x => x.ClientInfos.IPAddress == newPeer.ClientInfos.IPAddress);
				}

				Peers.Add(newPeer);
			}
		}

		void OnPing (MessageToHandle<Ping> sender, MessageEventArgs<Ping> e)
		{
			HandleMessage(new Log("Ping received for " + e.Message.PingId.ToString()), e.PeerToken, Guid.Empty, false);
			Send(new Pong(e.Message.PingId), e.PeerToken);
		}

		void OnPong (MessageToHandle<Pong> sender, MessageEventArgs<Pong> e)
		{
			HandleMessage(new Log("Pong received for " + e.Message.PingId.ToString()), e.PeerToken, Guid.Empty, false);
		}

		public static Type FindType (string typeFullName)
		{
			var typeInfo = typeof(Message).GetTypeInfo().Assembly.DefinedTypes.FirstOrDefault(x => x.FullName == typeFullName);
			return typeInfo != null ? typeInfo.AsType() : null;
		}

		void OnConnectWith (MessageToHandle<ConnectWith> sender, MessageEventArgs<ConnectWith> e)
		{
			var peers = e.Message.SharedPeers;

			var otherPeer = new Peer 
			{
				ClientInfos = e.Message.ClientInfos,
				Token = Guid.NewGuid()
			};

			var peersList = peers.ToList();
			peersList.RemoveAll(x => x.ClientInfos.IPAddress == otherPeer.ClientInfos.IPAddress);
			peersList.Add(otherPeer);
			AddPeers(peersList.ToArray());

			var myPeer = new Peer
			{
				ClientInfos = this.ClientInfos,
				Token = Guid.NewGuid()	
			};

			Send(new ReturnPeer(myPeer, Peers.ToArray()), otherPeer.Token, null);
		}

		void OnReturnPeer (MessageToHandle<ReturnPeer> sender, MessageEventArgs<ReturnPeer> e)
		{
			var peers = e.Message.SharedPeers;
			var peer = e.Message.ReturnedPeer;
			var peersList = peers.ToList();
			peersList.RemoveAll(x => x.ClientInfos.IPAddress == peer.ClientInfos.IPAddress);
			peersList.Add(peer);
			AddPeers(peersList.ToArray());
			Broadcast(new BroadcastPeers(Peers.ToArray()));
		}

		private void AddPeers (Peer[] peers)
		{
			foreach (var newPeer in peers)
			{
				if (Peers.Any (x => x.ClientInfos.IPAddress == newPeer.ClientInfos.IPAddress))
				{
					Peers.RemoveAll(x => x.ClientInfos.IPAddress == newPeer.ClientInfos.IPAddress);
				}

				Peers.Add(newPeer);
			}
		}

		private async Task<byte[]> ReadOrTimeout (ITcpSocketClient client, int bufferLen, TimeSpan timeout)
		{
			var buffer = new byte[bufferLen];
			client.ReadStream.ReadTimeout = (int)timeout.TotalMilliseconds;
			int read = 0, start = 0;

			while (read < bufferLen)
			{
				var count = client.ReadStream.Read (buffer, read, bufferLen - read);
				read += count;
				if (count == 0 && read < bufferLen)
				{
					throw new Exception("OOPS");
					break;
				}
			}

			return buffer;
		}

		private static byte[] IntToBytes (int intValue)
		{
			byte[] bytes = new byte[4];
			bytes[0] = (byte)(intValue >> 24);
			bytes[1] = (byte)(intValue >> 16);
			bytes[2] = (byte)(intValue >> 8);
			bytes[3] = (byte)intValue;
			return bytes;
		}

		private static bool DataMatch (byte[] a1, byte[] a2)
		{
			if (a1.Length != a2.Length)
				return false;

			for (int i = 0; i < a1.Length; i++)
				if (a1 [i] != a2 [i])
					return false;

			return true;
		}

		public async Task ConnectWith (ClientInfos destinationClientInfos)
		{
			await Send(new ConnectWith(ClientInfos, Peers.ToArray()), Guid.Empty, destinationClientInfos);
		}

		public async Task Send (object message)
		{
			await Send(message, Guid.NewGuid(), null);
		}

		public async Task Broadcast (object message)
		{
			await Send(message, Guid.NewGuid(), null, null);
		}

		public async Task Send (object message, Guid peerToken, Guid? messageToken = null)
		{
			var peer = Peers.FirstOrDefault (x => x.Token == peerToken);

			if (peerToken != Guid.Empty && peer == null)
			{
				return;
			}

			await Send(message, messageToken, peer.ClientInfos, peer.Token);
		}

		public async Task Send (object message, Guid? messageToken, ClientInfos destinationClientInfos, Guid? peerToken = null)
		{
			var messageEnveloppe = new MessageEnveloppe (message);
			var willBroadcast = destinationClientInfos == null && peerToken == null;
			var requestTypeSignature = willBroadcast ? Client.BroadcastSignature : Client.MessageSignature;
			var jsonContent = JsonConvert.SerializeObject (messageEnveloppe);
			var lenInBytes = Client.IntToBytes (jsonContent.Length);

			var dataBuilder = new List<byte> ();
			dataBuilder.AddRange (requestTypeSignature);
			dataBuilder.AddRange (messageToken.GetValueOrDefault ().ToByteArray ());
			dataBuilder.AddRange (peerToken.GetValueOrDefault ().ToByteArray ());
			dataBuilder.AddRange (lenInBytes);
			dataBuilder.AddRange (System.Text.Encoding.UTF8.GetBytes (jsonContent));

			var allClientInfos = new List<ClientInfos> ();

			if (willBroadcast)
			{
				allClientInfos = Peers.Select (x => x.ClientInfos).ToList ();
			} else
			{
				allClientInfos.Add (destinationClientInfos);
			}

			foreach (var clientInfos in allClientInfos)
			{
				using (var client = new TcpSocketClient ())
				{
					await client.ConnectAsync(clientInfos.IPAddress, clientInfos.Port);
					var data = dataBuilder.ToArray();
					await client.WriteStream.WriteAsync(data, 0, data.Length);
					await client.WriteStream.FlushAsync ();
				}
			}
		}

		RegistrationDictionary Registrations = new RegistrationDictionary();

		public MessageToHandle<T> AddHandler<T>(T messageObject) where T : class
		{ 
			var messageToHandle = new MessageToHandle<T>();
			Registrations.Add(messageObject.GetType(), messageToHandle);
			return messageToHandle as MessageToHandle<T>;
		}

		public HashSet<Guid> Broadcasted = new HashSet<Guid>();

		public void HandleMessage (object messageObject, Guid peerToken, Guid? messageToken, bool isBroadcast = false)
		{
			if (isBroadcast)
			{
				Broadcasted.Add (messageToken.GetValueOrDefault ());

				foreach (var peer in Peers)
				{
					Send(messageObject, messageToken, peer.ClientInfos, peer.Token);	
				}
			}

			var messageType = messageObject.GetType ();

			if (Registrations.ContainsRegistration (messageType))
			{
				var eventHandlerCollection = ((IEnumerable)Registrations.GetObject (messageType));

				foreach (var eventHandler in eventHandlerCollection)
				{
					var onMessageEventMethod = eventHandler.GetType().GetRuntimeMethods().FirstOrDefault(x => x.Name == "OnMessageEvent");
					var messageEventArgs = Activator.CreateInstance(onMessageEventMethod.GetParameters()[0].ParameterType);

					var messageProperty = messageEventArgs.GetType().GetRuntimeProperty("Message");
					messageProperty.SetValue(messageEventArgs, messageObject, null);

					var peerTokenProperty = messageEventArgs.GetType().GetRuntimeProperty("PeerToken");
					peerTokenProperty.SetValue(messageEventArgs, peerToken, null);

					onMessageEventMethod.Invoke(eventHandler, new object[1] {messageEventArgs});
				}
			}
		}
	}
}

