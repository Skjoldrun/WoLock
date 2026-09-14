using System.Net;
using System.Net.Sockets;
using Serilog;

namespace WoLock.Core;

/// <summary>
/// Minimal abstraction over the UDP transport so the sender can be tested
/// without opening real sockets.
/// </summary>
public interface IWakeUdpClient : IDisposable
{
    /// <summary>Enables or disables broadcast sending. Throws when the platform
    /// does not allow it.</summary>
    bool EnableBroadcast { get; set; }

    /// <summary>Sends the packet to the endpoint.</summary>
    ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, IPEndPoint endpoint, CancellationToken cancellationToken);
}

/// <summary>
/// Sends Wake-on-LAN magic packets over UDP.
/// </summary>
public interface IWakeSender
{
    /// <summary>
    /// Sends the magic packet for the request to every target endpoint.
    /// </summary>
    /// <param name="request">The wake request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="WakeSendException">
    /// Thrown when broadcast cannot be enabled or a packet cannot be sent.
    /// </exception>
    Task SendWakePacketAsync(WakeRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Concrete <see cref="IWakeSender"/> backed by <see cref="UdpClient"/>.</summary>
public sealed class WakeSender : IWakeSender
{
    private readonly ILogger _logger;
    private readonly Func<IWakeUdpClient> _clientFactory;

    /// <summary>
    /// Creates a sender.
    /// </summary>
    /// <param name="logger">Optional logger. Defaults to the global Serilog logger.</param>
    /// <param name="clientFactory">
    /// Factory for UDP clients. Injectable for tests; defaults to <see cref="UdpClient"/>.
    /// </param>
    public WakeSender(ILogger? logger = null, Func<IWakeUdpClient>? clientFactory = null)
    {
        _logger = logger ?? Log.Logger;
        _clientFactory = clientFactory ?? (() => new UdpClientAdapter());
    }

    /// <inheritdoc />
    public async Task SendWakePacketAsync(WakeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        byte[] packet = MagicPacket.Create(request.Mac);

        using IWakeUdpClient client = _clientFactory();
        try
        {
            client.EnableBroadcast = request.Broadcast;
        }
        catch (SocketException ex)
        {
            _logger.Error(
                ex,
                "Broadcast cannot be enabled on this platform/interface ({Broadcast}). Aborting.",
                request.Broadcast);
            throw new WakeSendException("Broadcast cannot be enabled on this platform/interface.", ex);
        }

        foreach (IPEndPoint endpoint in request.Targets)
        {
            try
            {
                await client.SendAsync(packet.AsMemory(), endpoint, cancellationToken).ConfigureAwait(false);
                _logger.Debug("Sent wake packet to {Endpoint} for {Mac}.", endpoint, request.Mac);
            }
            catch (SocketException ex)
            {
                _logger.Error(ex, "Failed to send wake packet to {Endpoint}.", endpoint);
                throw new WakeSendException($"Failed to send wake packet to {endpoint}.", ex);
            }
        }
    }
}

/// <summary><see cref="IWakeUdpClient"/> adapter over <see cref="UdpClient"/>.</summary>
internal sealed class UdpClientAdapter : IWakeUdpClient
{
    private readonly UdpClient _client = new();

    public bool EnableBroadcast
    {
        get => _client.EnableBroadcast;
        set => _client.EnableBroadcast = value;
    }

    public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, IPEndPoint endpoint, CancellationToken cancellationToken) =>
        _client.SendAsync(buffer, endpoint, cancellationToken);

    public void Dispose() => _client.Dispose();
}

/// <summary>Thrown when a wake packet cannot be sent.</summary>
public sealed class WakeSendException : Exception
{
    /// <summary>
    /// Creates a <see cref="WakeSendException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public WakeSendException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
