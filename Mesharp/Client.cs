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
		private object ApplicationContext;

		public static Client Create (string ipAddress, int port, string platform, string friendlyName, object applicationContext)
		{
			return new Client(ipAddress, port, platform, friendlyName, applicationContext);
		}

		public static byte[] MessageSignature = new byte[16] { 110, 108, 89, 202, 220, 203, 79, 76, 156, 153, 160, 132, 85, 194, 39, 233 };

		public static byte[] BroadcastSignature = new byte[16] { 76, 246, 208, 105, 232, 113, 153, 72, 183, 13, 138, 24, 124, 236, 199, 254 };

		public List<Peer> Peers = new List<Peer>();

		public List<Guid> BroadcastMessageHistory = new List<Guid>();

		public ClientInfos ClientInfos { get; set; }

		public Client (string ipAddress, int port, string platform, string friendlyName, object applicationContext)
		{
			ApplicationContext = applicationContext;

			ClientInfos = new ClientInfos()
			{
				IPAddress = ipAddress,
				Port = port,
				Platform = platform,
				PeerToken = Guid.NewGuid(),
				ConnectedOn = DateTimeOffset.UtcNow
			};

			AddHandler(new ConnectWith()).Received += OnConnectWith;
			AddHandler(new ReturnPeer()).Received += OnReturnPeer;
			AddHandler(new Ping()).Received += OnPing;
			AddHandler(new Pong()).Received += OnPong;
			AddHandler(new BroadcastPeers()).Received += OnBroadcastPeers;

			Init();
		}



		public class Request : Request<Message>
		{
			public Request(Guid messageToken) : base(messageToken){}
		}

		public class Request<T> where T : class
		{
			public Guid MessageToken { get; set; }

			public Request(Guid messageToken)
			{
				MessageToken = messageToken;
			}

			private event Action<T> _response;
			public event Action<T> Response {
				add 
				{
					_response += value;
				}
				remove
				{
					_response -= value;
				}
			}

			public virtual void Do(T response)
		    {
				_response(response);
		    }
		}

		private async Task Init ()
		{
			var listenPort = ClientInfos.Port;

			var listener = new TcpSocketListener ();

			Peer remotePeerForErrors = null;

			listener.ConnectionReceived += async (sender, args) => {
				Task.Run (async () => {
					using (var client = args.SocketClient)
					{
						remotePeerForErrors = Peers.FirstOrDefault (x => client.RemoteAddress.ToString ().ToLower ().Contains (x.ClientInfos.IPAddress.ToLower ()));
						if (remotePeerForErrors == null)
						{
							remotePeerForErrors = new Peer () {
								ClientInfos = new ClientInfos {
									FriendlyName = "",
									IPAddress = client.RemoteAddress,
									Port = client.RemotePort,
									PeerToken = Guid.Empty,
									Platform = "",
									ConnectedOn = DateTimeOffset.MinValue
								}
							};
						}

						if (!client.ReadStream.CanRead)
						{
							await client.DisconnectAsync ();
							throw new ErrorException (ErrorReason.OnConnection, "Cannot read remote stream");
						}

						var peerToken = Guid.Empty;
						var messageToken = Guid.Empty;
						var bytesReadSoFar = new List<byte> (); // For Exception debugging purpose
						var typeFullName = string.Empty;
						try
						{
							var sixteenBytesBuffer = new byte[16];
							var lenBuffer = new byte[4];
							var isBroadcast = false;
							var isValid = false;

							try
							{
								sixteenBytesBuffer = await ReadOrTimeout (client, sixteenBytesBuffer.Length, TimeSpan.FromSeconds (2)).ConfigureAwait (true);
								bytesReadSoFar.AddRange (sixteenBytesBuffer.ToArray ());
							} catch (Exception e)
							{
								throw new ErrorException (ErrorReason.OnReadGuid, e.Message);
							}

							if (DataMatch (sixteenBytesBuffer, MessageSignature))
							{
								isValid = true;
							}

							if (DataMatch (sixteenBytesBuffer, BroadcastSignature))
							{
								isValid = true;
								isBroadcast = true;
							}

							if (!isValid)
							{
								throw new ErrorException (ErrorReason.OnMessageSignature, "Invalid message signature");
							}

							try
							{
								sixteenBytesBuffer = await ReadOrTimeout (client, sixteenBytesBuffer.Length, TimeSpan.FromSeconds (2)).ConfigureAwait (false);
								bytesReadSoFar.AddRange (sixteenBytesBuffer.ToArray ());
								messageToken = new Guid (sixteenBytesBuffer);
							} catch (Exception e)
							{
								throw new ErrorException (ErrorReason.OnReadMessageToken, e.Message);
							}

							if (messageToken != Guid.Empty && Broadcasted.Contains (messageToken))
							{
								await client.DisconnectAsync ();
								return;
							}

							try
							{
								sixteenBytesBuffer = await ReadOrTimeout (client, sixteenBytesBuffer.Length, TimeSpan.FromSeconds (2)).ConfigureAwait (false);
								bytesReadSoFar.AddRange (sixteenBytesBuffer.ToArray ());
								peerToken = new Guid (sixteenBytesBuffer);
							} catch (Exception e)
							{
								throw new ErrorException (ErrorReason.OnReadMessageToken, e.Message);
							}

							try
							{
								lenBuffer = await ReadOrTimeout (client, lenBuffer.Length, TimeSpan.FromSeconds (2)).ConfigureAwait (false);
								bytesReadSoFar.AddRange (lenBuffer.ToArray ());
								if (BitConverter.IsLittleEndian)
								{
									Array.Reverse (lenBuffer);
								}
							} catch (Exception e)
							{
								throw new ErrorException (ErrorReason.OnReadMessageLength, e.Message);
							}

							var len = BitConverter.ToInt32 (lenBuffer, 0);

							var contentBuffer = new byte[len];

							try
							{
								contentBuffer = await ReadOrTimeout (client, contentBuffer.Length, TimeSpan.FromSeconds (2));
								bytesReadSoFar.AddRange (contentBuffer.ToArray ());
							} catch (Exception e)
							{
								throw new ErrorException (ErrorReason.OnReadContent, e.Message);
							}

							JObject deserialized = null;

							try
							{
								var json = System.Text.Encoding.UTF8.GetString (contentBuffer, 0, contentBuffer.Length);
								deserialized = JsonConvert.DeserializeObject (json) as JObject;
							} catch (Exception e)
							{
								throw new ErrorException (ErrorReason.OnDeserialize, e.Message);
							}

							if (deserialized != null)
							{
								typeFullName = (deserialized.GetValue ("TypeFullName") as JToken).ToString ();
								var innerMessage = (deserialized.GetValue ("Message") as JToken).ToString ();
								var typedMessage = JsonConvert.DeserializeObject (innerMessage, FindType (typeFullName));

								// If the peertoken is not part of the list and non-empty, and we are not broadcasting, it is totally irrelevant for us.
								if (peerToken != Guid.Empty && !Peers.Any (x => x.ClientInfos.PeerToken == peerToken) && !isBroadcast && typeFullName != typeof(ReturnPeer).FullName)
								{
									throw new ErrorException (ErrorReason.PeerNotFound, "Peer not found");
								}

								if (null == HandleMessage (typedMessage, peerToken, new Guid? (messageToken), isBroadcast))
								{
									throw new ErrorException (ErrorReason.NoHandlerForMessage, "This peer cannot handle your message of type " + typeFullName + " :" + (deserialized.ToString ().Length < 4096 ? (": " + deserialized.ToString ()) : ""));
								}

								try
								{
									HandleMessage (new Log (deserialized.ToString ()), peerToken, messageToken, false);
								} catch (Exception e)
								{
									throw new ErrorException (ErrorReason.OnHandlingMessage, e.Message);
								}
							}

						} catch (ErrorException e)
						{
							try
							{
								if (typeFullName == "Mesharp.BadRequest")
								{
									return;
								}
								var badRequest = new BadRequest {
									BytesAsBase64 = Convert.ToBase64String (bytesReadSoFar.Count < 4096 ? bytesReadSoFar.ToArray () : new byte[1]),
									MessageToken = messageToken,
									RemoteClientInfos = ClientInfos,
									ErrorReason = e.Reason,
									ErrorMessage = e.Message
								};

								bytesReadSoFar.Clear ();
								HandleMessage (new BadRequestReport (badRequest), Guid.Empty, Guid.Empty, false);
								Send (badRequest, remotePeerForErrors.ClientInfos.PeerToken);
							} catch
							{
								// Silence radio.
							}
						} catch (Exception e)
						{
							if (typeFullName == "Mesharp.BadRequest")
							{
								return;
							}
							HandleMessage (new UnhandledException (e), Guid.Empty, Guid.Empty, false);
						} finally
						{
							await client.DisconnectAsync ();
						}
					}
				});

			};

			await listener.StartListeningAsync (listenPort);
		}

		// TODO: Older clientInfos could override newer clientInfos. Priority should be based on a date
		void OnBroadcastPeers (MessageToHandle<BroadcastPeers> sender, MessageEventArgs<BroadcastPeers> e)
		{
			AddAndCleanupPeers(e.Message.Peers);
		}

		void OnPing (MessageToHandle<Ping> sender, MessageEventArgs<Ping> e)
		{
			HandleMessage(new Log("Ping received for " + e.Message.PingId.ToString()), e.PeerToken, Guid.Empty, false);
			Send(new Pong(e.Message.PingId), e.PeerToken, e.MessageToken);
		}

		void OnPong (MessageToHandle<Pong> sender, MessageEventArgs<Pong> e)
		{
			HandleMessage(new Log("Pong received for " + e.Message.PingId.ToString()), e.PeerToken, Guid.Empty, false);
		}

		public Type FindType (string typeFullName)
		{
			var knownTypes = ApplicationContext.GetType().GetTypeInfo().Assembly.DefinedTypes.ToList(); 
			var mesharpDefinedTypes = this.GetType().GetTypeInfo().Assembly.DefinedTypes.ToList();
			knownTypes.AddRange(mesharpDefinedTypes);
			var typeInfo = knownTypes.FirstOrDefault(x => x.FullName == typeFullName);
//			var typeInfo = typeof(Message).GetTypeInfo().Assembly.DefinedTypes.FirstOrDefault(x => x.FullName == typeFullName);
			return typeInfo != null ? typeInfo.AsType() : null;
		}

		void OnConnectWith (MessageToHandle<ConnectWith> sender, MessageEventArgs<ConnectWith> e)
		{
			var peers = e.Message.SharedPeers;

			var otherPeer = new Peer 
			{
				ClientInfos = e.Message.ClientInfos,
			};

			AddAndCleanupPeers(peers, otherPeer);

			var myPeer = new Peer
			{
				ClientInfos = this.ClientInfos
			};

			var ret = Send<ReturnPeer, ReturnPeer>(new ReturnPeer(myPeer, Peers.ToArray()), otherPeer.ClientInfos.PeerToken, null);
		}

		void OnReturnPeer (MessageToHandle<ReturnPeer> sender, MessageEventArgs<ReturnPeer> e)
		{
			var peers = e.Message.SharedPeers;
			var peer = e.Message.ReturnedPeer;
			AddAndCleanupPeers(peers, peer);
			Broadcast(new BroadcastPeers(Peers.ToArray()));
		}

		void AddAndCleanupPeers (Peer[] otherPeers, Peer otherPeer = null)
		{
			var unsortedPeers = Peers.ToList ();

			if (otherPeer != null)
			{
				unsortedPeers.Add(otherPeer);
			}

			unsortedPeers.AddRange(otherPeers);
			Peers = unsortedPeers.GroupBy(x => x.ClientInfos.IPAddress).Select(x => x.OrderByDescending(y => y.ClientInfos.ConnectedOn).FirstOrDefault()).ToList();
		}


		private async Task<byte[]> ReadOrTimeout (ITcpSocketClient client, int bufferLen, TimeSpan timeout)
		{
			var buffer = new byte[bufferLen];
			client.ReadStream.ReadTimeout = (int)timeout.TotalMilliseconds;

			var read = 0;

			while (read < bufferLen)
			{
				var count = client.ReadStream.Read (buffer, read, bufferLen - read);
				read += count;
				if (count == 0 && read < bufferLen)
				{
					throw new Exception("Error while reading stream from remote client");
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
			{
				return false;
			}

			for (int i = 0; i < a1.Length; i++)
			{
				if (a1 [i] != a2 [i])
				{
					return false;
				}
			}
			return true;
		}

		public Request<ReturnPeer> ConnectWith(ClientInfos destinationClientInfos)
		{
			return Send<ConnectWith, ReturnPeer>(new ConnectWith(ClientInfos, Peers.ToArray()), Guid.Empty, destinationClientInfos);
		}

		public Request<Message> Send<Req> (Req message) where Req : class {return Send<Req,Message>(message, Guid.NewGuid(), null);}
		public Request<Resp> Send<Req, Resp> (Req message) where Req : class where Resp : class, new()
		{
			return Send<Req, Resp>(message, Guid.Empty, Guid.NewGuid());
		}

		public Request<Message> Broadcast<Req> (Req message) where Req : class 
		{
			return Send<Req, Message>(message);
		}
		public Request<Resp> Broadcast<Req, Resp> (Req message) where Req : class where Resp : class, new()
		{
			return Send<Req, Resp>(message, Guid.Empty,  Guid.NewGuid());
		}

		public Request<Message> Send<Req> (Req message, Guid peerToken, Guid? messageToken = null) where Req : class {return Send<Req, Message>(message, peerToken, messageToken);}
		public Request<Resp> Send<Req, Resp> (Req message, Guid peerToken, Guid? messageToken = null) where Req : class where Resp : class, new()
		{
			var peer = Peers.FirstOrDefault (x => x.ClientInfos.PeerToken == peerToken) ?? new Peer();

			if (peerToken != Guid.Empty && peer.ClientInfos == null)
			{
				return new Request<Resp>(Guid.Empty);
			}

			return Send<Req, Resp>(message, messageToken, peer.ClientInfos);
		}

		public Request<Message> Send <Req> (Req message, Guid? messageToken, ClientInfos destinationClientInfos) where Req : class{return Send<Req,Message>(message, messageToken, destinationClientInfos);}
		public Request<Resp> Send <Req, Resp> (Req message, Guid? messageToken, ClientInfos destinationClientInfos) where Req : class where Resp : class, new()
		{
			var messageEnveloppe = new MessageEnveloppe (message);
			var willBroadcast = destinationClientInfos == null;
			var requestTypeSignature = willBroadcast ? Client.BroadcastSignature : Client.MessageSignature;
			var jsonContent = JsonConvert.SerializeObject (messageEnveloppe);
			var lenInBytes = Client.IntToBytes (jsonContent.Length);
			var destinationPeerToken = willBroadcast ? Guid.Empty : destinationClientInfos.PeerToken;

			var dataBuilder = new List<byte> ();
			dataBuilder.AddRange (requestTypeSignature);
			dataBuilder.AddRange (messageToken.GetValueOrDefault ().ToByteArray ());
			dataBuilder.AddRange (destinationPeerToken.ToByteArray ());
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

			Task.Run (async () => {
				foreach (var clientInfos in allClientInfos)
				{
					using (var client = new TcpSocketClient ())
					{
						await client.ConnectAsync (clientInfos.IPAddress, clientInfos.Port);
						var data = dataBuilder.ToArray ();
						await client.WriteStream.WriteAsync (data, 0, data.Length);
						await client.WriteStream.FlushAsync ();
					}
				}
			});

			var mToken = messageToken.GetValueOrDefault();
			if (mToken == Guid.Empty) mToken = Guid.NewGuid();

			return new Request<Resp>(mToken);
		}

		GenericsDictionary<Type> DelegateList = new GenericsDictionary<Type>();

		public MessageToHandle<T> AddHandler<T>(T messageObject) where T : class
		{
			var messageToHandle = new MessageToHandle<T>();
			DelegateList.Add(messageObject.GetType(), messageToHandle);
			return messageToHandle as MessageToHandle<T>;
		}

		public HashSet<Guid> Broadcasted = new HashSet<Guid>();

		public object HandleMessage (object messageObject, Guid peerToken, Guid? messageToken, bool isBroadcast = false)
		{
			if (isBroadcast)
			{
				Broadcasted.Add (messageToken.GetValueOrDefault ());

				foreach (var peer in Peers)
				{
					Send (messageObject, messageToken, peer.ClientInfos);	
				}
			}

			var messageType = messageObject.GetType ();

//			// In simple words: If a peer re-sent a message with the same messageToken,
//			// then it is considered as a response, and it invokes an action.
//			if (messageToken.GetValueOrDefault() != Guid.Empty && RequestList.ContainsItem (messageToken.GetValueOrDefault()))
//			{
//				var obj = RequestList.GetObject(messageToken.GetValueOrDefault());
//				var test = obj.GetType().GetRuntimeMethods();
//			}

			if (DelegateList.ContainsItem (messageType))
			{
				var eventHandlerCollection = ((IEnumerable)DelegateList.GetObject (messageType));

				foreach (var eventHandler in eventHandlerCollection)
				{
					var onMessageEventMethod = eventHandler.GetType ().GetRuntimeMethods ().FirstOrDefault (x => x.Name == "OnMessageEvent");
					var messageEventArgs = Activator.CreateInstance (onMessageEventMethod.GetParameters () [0].ParameterType);

					var messageProperty = messageEventArgs.GetType ().GetRuntimeProperty ("Message");
					messageProperty.SetValue (messageEventArgs, messageObject, null);

					var peerTokenProperty = messageEventArgs.GetType ().GetRuntimeProperty ("PeerToken");
					peerTokenProperty.SetValue (messageEventArgs, peerToken, null);

					var messageTokenProperty = messageEventArgs.GetType ().GetRuntimeProperty ("MessageToken");
					messageTokenProperty.SetValue (messageEventArgs, messageToken.GetValueOrDefault(), null);

					onMessageEventMethod.Invoke (eventHandler, new object[1] { messageEventArgs });
				}

				return new Message("OK");
			}
			return null;
		}
	}
}