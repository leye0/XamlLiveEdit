using System;
using System.Linq;
using Sockets.Plugin;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Sockets.Plugin.Abstractions;
using System.Reflection;

namespace Mesharp
{
	public class Peer
	{
		public ClientInfos ClientInfos { get; set; }

		public Guid Token { get; set; }
	}

	public class Client
	{
		public static Client Create (string ipAddress, int port, string platform)
		{
			return new Client(ipAddress, port, platform);
		}

		public static byte[] MessageSignature = new byte[16] { 110, 108, 89, 202, 220, 203, 79, 76, 156, 153, 160, 132, 85, 194, 39, 233 };

		public static byte[] BroadcastSignature = new byte[16] { 76, 246, 208, 105, 232, 113, 153, 72, 183, 13, 138, 24, 124, 236, 199, 254 };

		public List<Peer> Peers = new List<Peer>();

		public List<Guid> BroadcastMessageHistory = new List<Guid>();

		public ClientInfos ClientInfos { get; set; }

		public Client (string ipAddress, int port, string platform)
		{
			ClientInfos = new ClientInfos()
			{
				IPAddress = ipAddress,
				Port = port,
				Platform = platform,
			};

			Init();
		}

		private async Task Init ()
		{
			var listenPort = ClientInfos.Port;

			var listener = new TcpSocketListener ();

			RegisterMessageEventHandler(new ConnectWith()).EventHandler += OnConnectWith;
			RegisterMessageEventHandler(new ReturnPeer()).EventHandler += OnReturnPeer;

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

						if (isBroadcast && BroadcastMessageHistory.Contains(messageToken))
						{
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

						// DEBUG:
						var json = System.Text.Encoding.UTF8.GetString(contentBuffer, 0, contentBuffer.Length);
						// TryDeserialize
						var rawOutput = new object();
						var deserialized = SimpleJson.TryDeserializeObject(json, out rawOutput);
						if (deserialized)
						{
							var message = SimpleJson.DeserializeObject<Message>(json);

							// If the peertoken is not part of the list and non-empty, and we are not broadcasting, it is totally irrelevant for us.
							if (peerToken != Guid.Empty && !Peers.Any(x => x.Token == peerToken) && !isBroadcast && message.Kind != typeof(ReturnPeer).FullName)
							{
								return;
							}

							var messageType = typeof(Message).GetTypeInfo().Assembly.DefinedTypes.FirstOrDefault(x => x.FullName.ToLower() == message.Kind.ToLower());
							var messageTyped = Activator.CreateInstance(messageType.AsType()) as Message;
							var contentType = typeof(Message).GetTypeInfo().Assembly.DefinedTypes.FirstOrDefault(x => x.FullName.ToLower() == message.ContentType.ToLower());

							var content = SimpleJson.DeserializeObject(message.Content.ToString(), contentType.AsType());

							(messageTyped as Message).Content = content;
							(messageTyped as Message).Kind = message.Kind;
							HandleMessage(messageTyped, peerToken);
						}

					} 
					catch (Exception e)
					{
						var ab = e.Message;
					}
					finally
					{
						await client.DisconnectAsync();
					}
				}
			};

			// bind to the listen port across all interfaces
			await listener.StartListeningAsync (listenPort);
		}

		void OnReturnPeer (MessageToHandle<ReturnPeer> sender, MessageEventArgs<ReturnPeer> e)
		{
			Peers.Add((Peer) e.Message.Content);
		}

		void OnConnectWith (MessageToHandle<global::Mesharp.ConnectWith> sender, MessageEventArgs<global::Mesharp.ConnectWith> eventArgs)
		{
			var clientInfo = (ClientInfos)eventArgs.Message.Content;

			var newPeerToken = Guid.NewGuid();

			var peer = new Peer 
			{
				ClientInfos = clientInfo,
				Token = newPeerToken
			};

			if (!Peers.Any (x => x.Token == peer.Token))
			{
				Peers.Add(peer);
			}

			Send(new ReturnPeer(new Peer { ClientInfos = this.ClientInfos, Token = newPeerToken }), newPeerToken, null);
		}

		private async Task<byte[]> ReadOrTimeout (ITcpSocketClient client, int bufferLen, TimeSpan timeout)
		{
			var buffer = new byte[bufferLen];
			var success = false;
			var startWait = DateTime.Now;

			client.ReadStream.ReadTimeout = (int) timeout.TotalMilliseconds;
			client.ReadStream.Read(buffer, 0, buffer.Length);
			success = true;

			await Task.Run(async () => 
			{
				while(!success)
				{
					await Task.Delay (10);
					if (DateTime.Now - startWait > timeout)
					{
						client.ReadStream.Dispose();
						client.DisconnectAsync();
						throw new Exception("Timeout. Disconnect client.");
					}
				}
			});

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
			await Send(new ConnectWith(ClientInfos), Guid.NewGuid(), destinationClientInfos);
		}

		public async Task Send (Message message, Guid peerToken, Guid? messageToken = null)
		{
			var peer = Peers.FirstOrDefault (x => x.Token == peerToken);

			if (peerToken != Guid.Empty && peer == null)
			{
				return;
			}

			await Send(message, messageToken, peer.ClientInfos, peer.Token);
		}

		public async Task Send (Message message, Guid? messageToken, ClientInfos destinationClientInfos, Guid? peerToken = null)
		{
			var requestTypeSignature = Client.MessageSignature;
			var jsonContent = message.AsJson();
			var lenInBytes = Client.IntToBytes (jsonContent.Length);

			var dataBuilder = new List<byte> ();
			dataBuilder.AddRange (requestTypeSignature);
			dataBuilder.AddRange (messageToken.GetValueOrDefault ().ToByteArray ());
			dataBuilder.AddRange (peerToken.GetValueOrDefault ().ToByteArray ());
			dataBuilder.AddRange (lenInBytes);
			dataBuilder.AddRange (System.Text.Encoding.UTF8.GetBytes(jsonContent));

			using (var client = new TcpSocketClient ())
			{
				await client.ConnectAsync(destinationClientInfos.IPAddress, destinationClientInfos.Port);
				var data = dataBuilder.ToArray();
				await client.WriteStream.WriteAsync(data, 0, data.Length);
				await client.WriteStream.FlushAsync ();
			}
		}

//		public async Task Broadcast(Message message, object content)
//		{
//			
//		}
		

		RegistrationDictionary Registrations = new RegistrationDictionary();

		public MessageToHandle<T> RegisterMessageEventHandler<T>(T messageObject) where T : Message
		{ 
			if (Registrations.ContainsRegistration (messageObject.GetType()))
			{
				throw new Exception("Message type already registred");
			}

			var messageToHandle = new MessageToHandle<T>();
			Registrations.Add(messageObject.GetType(), messageToHandle);
			return messageToHandle as MessageToHandle<T>;
		}

		public void HandleMessage(IMessage messageObject, Guid peerToken)
		{
			var key = messageObject.GetType ();

			if (Registrations.ContainsRegistration (key))
			{
				var value = Registrations.GetObject(key);
				var onMessageEventMethod = value.GetType().GetRuntimeMethods().FirstOrDefault(x => x.Name == "OnMessageEvent");
				var messageEventArgs = Activator.CreateInstance(onMessageEventMethod.GetParameters()[0].ParameterType);

//				public T Message { get; set; }
//				public Guid PeerToken { get; set; }
//
				var messageProperty = messageEventArgs.GetType().GetRuntimeProperty("Message");
				messageProperty.SetValue(messageEventArgs, messageObject, null);

				var peerTokenProperty = messageEventArgs.GetType().GetRuntimeProperty("PeerToken");
				peerTokenProperty.SetValue(messageEventArgs, peerToken, null);


				onMessageEventMethod.Invoke(value, new object[1] {messageEventArgs});

//				value.OnMessageEvent(new MessageEventArgs<Message>() { Message = messageObject, PeerToken = peerToken } );
			}
		}

		public class RegistrationDictionary
		{
			private Dictionary<Type, object> _dict = new Dictionary<Type, object>();

			public void Add<T>(Type key, T value) where T : class
		    {
		        _dict.Add(key, value);
		    }

			public bool ContainsRegistration(Type key)
		    {
		        return _dict.ContainsKey(key);
		    }

			public T GetValue<T>(Type key) where T : class
		    {
				return _dict[key] as T;
		    }

			public object GetObject(Type key)
		    {
				return _dict[key];
		    }
		}
	}

	public delegate void MessageHandler<T, U>(T sender, U e) where T : class;

		public class MessageEventArgs<T> : System.EventArgs
		{ 
			public T Message { get; set; }
			public Guid PeerToken { get; set; }
		}

		public class MessageToHandle<T>
		{

			public event MessageHandler<MessageToHandle<T>, MessageEventArgs<T>> EventHandler;

			public virtual void OnMessageEvent(MessageEventArgs<T> a)
		    {
		        EventHandler(this, a);
		    }
		}

}

