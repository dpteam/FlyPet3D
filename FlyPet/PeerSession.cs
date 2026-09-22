using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FlyPet;

public sealed class PeerSession : IDisposable
{
	private sealed record Packet(string Kind, Guid Owner, long Sequence = 0L, int Protocol = 1, string? Key = null, FlyFrame? Frame = null, TableAction? Action = null);

	private const int Version = 1;

	private const int MaxPacket = 8192;

	private readonly Guid _localId;

	private readonly string _key;

	private readonly CancellationTokenSource _stop = new CancellationTokenSource();

	private readonly ConcurrentQueue<TableAction> _actions = new ConcurrentQueue<TableAction>();

	private readonly Channel<Packet> _outgoing = Channel.CreateBounded<Packet>(new BoundedChannelOptions(64)
	{
		SingleReader = true
	});

	private TcpListener? _listener;

	private TcpClient? _client;

	private X509Certificate2? _certificate;

	private FlyFrame? _remote;

	private string _status = "Подключение…";

	private int _connected;

	private int _disposed;

	private long _sequence;

	public bool Connected => Volatile.Read(ref _connected) == 1;

	public string Status => Volatile.Read(ref _status);

	public FlyFrame? Remote => Volatile.Read(ref _remote);

	public Guid RemoteId { get; private set; }

	public int Port { get; private set; }

	public string Fingerprint { get; private set; } = "";

	public string AdvertisedHost { get; private set; } = "";

	public Task Completion { get; private set; } = Task.CompletedTask;

	private PeerSession(Guid localId, string key)
	{
		_localId = localId;
		_key = key;
	}

	public static PeerSession Host(Guid owner, int port = 47831, IPAddress? bindAddress = null)
	{
		PeerSession peerSession = new PeerSession(owner, ConvertEx.ToHexString(RandomEx.GetBytes(24)));
		try
		{
			using RSA key = RSA.Create(2048);
			using X509Certificate2 x509Certificate = new CertificateRequest("CN=FlyPet direct room", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5.0), DateTimeOffset.UtcNow.AddDays(2.0));
			peerSession._certificate = X509CertificateLoaderEx.LoadPkcs12(x509Certificate.Export(X509ContentType.Pfx), null);
			peerSession.Fingerprint = peerSession._certificate.GetCertHashString(HashAlgorithmName.SHA256);
			peerSession._listener = new TcpListener(bindAddress ?? IPAddress.Any, port);
			peerSession._listener.Start(4);
			peerSession.Port = ((IPEndPoint)peerSession._listener.LocalEndpoint).Port;
			peerSession._status = "Ожидаем второго игрока";
			peerSession.Completion = Task.Run((Func<Task>)peerSession.Accept);
			return peerSession;
		}
		catch
		{
			peerSession.Dispose();
			throw;
		}
	}

	public string Invite(string reachableHost)
	{
		if (_listener == null)
		{
			throw new InvalidOperationException("Приглашение создаёт хозяин стола.");
		}
		string text = reachableHost.Trim();
		UriHostNameType uriHostNameType = Uri.CheckHostName(text);
		if (uriHostNameType == UriHostNameType.Unknown || uriHostNameType == UriHostNameType.IPv6)
		{
			throw new FormatException("Укажите IPv4-адрес или имя компьютера без порта.");
		}
		AdvertisedHost = text;
		return $"flypet://{text}:{Port}/{_key}#{Fingerprint}";
	}

	public static PeerSession Join(Guid owner, string invitation)
	{
		bool flag = !Uri.TryCreate(invitation.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "flypet";
		if (!flag)
		{
			int port = uri.Port;
			bool flag2 = ((port < 1 || port > 65535) ? true : false);
			flag = flag2;
		}
		if (!flag && uri.AbsolutePath.Trim('/').Length == 48 && uri.Fragment.Length == 65 && uri.AbsolutePath.Trim('/').All(Uri.IsHexDigit))
		{
			string fragment = uri.Fragment;
			if (fragment.Substring(1, fragment.Length - 1).All(Uri.IsHexDigit))
			{
				PeerSession session = new PeerSession(owner, uri.AbsolutePath.Trim('/'));
				session.Completion = Task.Run(delegate
				{
					PeerSession peerSession = session;
					string host = uri.Host;
					int port2 = uri.Port;
					string fragment2 = uri.Fragment;
					return peerSession.Connect(host, port2, fragment2.Substring(1, fragment2.Length - 1));
				});
				return session;
			}
		}
		throw new FormatException("Вставьте полное приглашение flypet://… от второго игрока.");
	}

	private async Task Accept()
	{
		try
		{
			while (!_stop.IsCancellationRequested)
			{
				TcpClient client = (_client = await _listener.AcceptTcpClientAsync(_stop.Token));
				try
				{
					client.NoDelay = true;
					using SslStream ssl = new SslStream(client.GetStream(), leaveInnerStreamOpen: false);
					using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
					timeout.CancelAfter(TimeSpan.FromSeconds(8.0));
					await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
					{
						ServerCertificate = _certificate,
						EnabledSslProtocols = (SslProtocols.Tls12 | SslProtocols.Tls13)
					}, timeout.Token);
					ValidateHello(await Read(ssl, timeout.Token));
					await Write(ssl, new Packet("hello", _localId, 0L, 1, _key), timeout.Token);
					await Exchange(ssl);
				}
				catch (Exception ex) when (((ex is IOException || ex is SocketException || ex is AuthenticationException || ex is JsonException || ex is InvalidDataException || ex is OperationCanceledException || ex is ObjectDisposedException) ? 1 : 0) != 0)
				{
					if (!_stop.IsCancellationRequested)
					{
						Volatile.Write(ref _status, "Связь закрыта. Ожидаем нового подключения.");
					}
				}
				finally
				{
					DisconnectPeer();
					client.Dispose();
					if (!_stop.IsCancellationRequested)
					{
						Volatile.Write(ref _status, "Ожидаем второго игрока");
					}
				}
			}
		}
		catch (Exception ex2) when (((ex2 is OperationCanceledException || ex2 is SocketException || ex2 is ObjectDisposedException) ? 1 : 0) != 0)
		{
		}
	}

	private async Task Connect(string host, int port, string fingerprint)
	{
		try
		{
			using TcpClient client = new TcpClient
			{
				NoDelay = true
			};
			_client = client;
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
			timeout.CancelAfter(TimeSpan.FromSeconds(10.0));
			await client.ConnectAsync(host, port, timeout.Token);
			using SslStream ssl = new SslStream(client.GetStream(), leaveInnerStreamOpen: false, (object _, X509Certificate? cert, X509Chain? _, SslPolicyErrors _) => cert != null && string.Equals(cert.GetCertHashString(HashAlgorithmName.SHA256), fingerprint, StringComparison.OrdinalIgnoreCase));
			await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
			{
				TargetHost = "FlyPet",
				EnabledSslProtocols = (SslProtocols.Tls12 | SslProtocols.Tls13)
			}, timeout.Token);
			await Write(ssl, new Packet("hello", _localId, 0L, 1, _key), timeout.Token);
			ValidateHello(await Read(ssl, timeout.Token));
			await Exchange(ssl);
			Volatile.Write(ref _status, "Игрок отключился. Можно подключиться снова.");
		}
		catch (Exception ex) when (((ex is IOException || ex is SocketException || ex is AuthenticationException || ex is JsonException || ex is InvalidDataException || ex is OperationCanceledException || ex is ObjectDisposedException) ? 1 : 0) != 0)
		{
			if (!_stop.IsCancellationRequested)
			{
				Volatile.Write(ref _status, (ex is InvalidDataException) ? ex.Message : "Нет соединения. Проверьте приглашение, VPN или проброс TCP-порта.");
			}
		}
		finally
		{
			DisconnectPeer();
		}
	}

	private void ValidateHello(Packet packet)
	{
		if (packet.Kind != "hello" || packet.Protocol != 1 || packet.Key != _key || packet.Owner == Guid.Empty)
		{
			throw new InvalidDataException("Неверное приглашение или версия игры.");
		}
		if (packet.Owner == _localId)
		{
			throw new InvalidDataException("Этот профиль уже используется. Для второго окна выберите другой --profile.");
		}
		RemoteId = packet.Owner;
	}

	private async Task Exchange(SslStream stream)
	{
		Packet item;
		while (_outgoing.Reader.TryRead(out item))
		{
		}
		TableAction result;
		while (_actions.TryDequeue(out result))
		{
		}
		Volatile.Write(ref _connected, 1);
		Volatile.Write(ref _status, "Второй игрок за столом");
		using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
		Task writer = Writer(stream, linked.Token);
		Task reader = Reader(stream, linked.Token);
		await Task.WhenAny(writer, reader).ConfigureAwait(continueOnCapturedContext: false);
		linked.Cancel();
		try
		{
			await Task.WhenAll(writer, reader).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException) when (linked.IsCancellationRequested)
		{
		}
	}

	private async Task Writer(SslStream stream, CancellationToken token)
	{
		while (await _outgoing.Reader.WaitToReadAsync(token).ConfigureAwait(continueOnCapturedContext: false))
		{
			Packet item;
			while (_outgoing.Reader.TryRead(out item))
			{
				await Write(stream, item, token).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	private async Task Reader(SslStream stream, CancellationToken token)
	{
		long last = 0L;
		while (!token.IsCancellationRequested)
		{
			using CancellationTokenSource idle = CancellationTokenSource.CreateLinkedTokenSource(token);
			idle.CancelAfter(TimeSpan.FromSeconds(12.0));
			Packet packet = await Read(stream, idle.Token);
			if (packet.Owner != RemoteId || packet.Protocol != 1 || packet.Sequence <= last)
			{
				throw new InvalidDataException("Нарушена принадлежность мухи клиенту.");
			}
			last = packet.Sequence;
			if (!(packet.Kind == "frame"))
			{
				goto IL_01d1;
			}
			FlyFrame frame = packet.Frame;
			if ((object)frame == null || !frame.IsValid(RemoteId))
			{
				goto IL_01d1;
			}
			Volatile.Write(ref _remote, frame);
			goto end_IL_0043;
			IL_01d1:
			if (!(packet.Kind == "action"))
			{
				goto IL_01c5;
			}
			TableAction action = packet.Action;
			if ((object)action == null || !action.IsValid || _actions.Count >= 128)
			{
				goto IL_01c5;
			}
			_actions.Enqueue(action);
			goto end_IL_0043;
			IL_01c5:
			throw new InvalidDataException("Некорректное состояние игрока.");
			end_IL_0043:;
		}
	}

	private static async Task Write(Stream stream, Packet packet, CancellationToken token)
	{
		byte[] data = JsonSerializer.SerializeToUtf8Bytes(packet);
		if (data.Length > 8192)
		{
			throw new InvalidDataException("Пакет слишком большой.");
		}
		byte[] array = new byte[4];
		BinaryPrimitives.WriteInt32BigEndian(array, data.Length);
		await stream.WriteAsync(array, token);
		await stream.WriteAsync(data, token);
	}

	private static async Task<Packet> Read(Stream stream, CancellationToken token)
	{
		byte[] header = new byte[4];
		await stream.ReadExactlyAsync(header, token);
		int num = BinaryPrimitives.ReadInt32BigEndian(header);
		if (num <= 0 || num > 8192)
		{
			throw new InvalidDataException("Некорректная длина пакета.");
		}
		byte[] data = new byte[num];
		await stream.ReadExactlyAsync(data, token);
		return JsonSerializer.Deserialize<Packet>(data) ?? throw new InvalidDataException("Пустой пакет.");
	}

	private bool Send(Packet packet)
	{
		if (!Connected)
		{
			return false;
		}
		if (_outgoing.Writer.TryWrite(packet))
		{
			return true;
		}
		Volatile.Write(ref _status, "Соединение перегружено. Подключитесь повторно.");
		_client?.Dispose();
		return false;
	}

	public bool SendFrame(FlyFrame frame)
	{
		if (frame.IsValid(_localId))
		{
			return Send(new Packet("frame", _localId, Interlocked.Increment(ref _sequence), 1, null, frame));
		}
		return false;
	}

	public bool SendAction(TableAction action)
	{
		if (action.IsValid)
		{
			return Send(new Packet("action", _localId, Interlocked.Increment(ref _sequence), 1, null, null, action));
		}
		return false;
	}

	public bool TryAction(out TableAction? action)
	{
		return _actions.TryDequeue(out action);
	}

	private void DisconnectPeer()
	{
		Volatile.Write(ref _connected, 0);
		Volatile.Write(ref _remote, null);
		TableAction result;
		while (_actions.TryDequeue(out result))
		{
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			_stop.Cancel();
			_client?.Dispose();
			_listener?.Stop();
			DisconnectPeer();
			Volatile.Write(ref _status, "Одиночная игра");
			Completion.ContinueWith(delegate
			{
				_certificate?.Dispose();
				_stop.Dispose();
			}, TaskScheduler.Default);
		}
	}
}
