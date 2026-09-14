using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace WoLock.Core;

/// <summary>Checks whether an IPv4 address responds to a ping.</summary>
public interface IPingChecker
{
    /// <summary>
    /// Sends a single ICMP echo request and waits for a reply.
    /// </summary>
    /// <param name="address">The address to ping.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>true</c> if the address responded; otherwise <c>false</c>.</returns>
    Task<bool> PingAsync(IPAddress address, int timeoutMs, CancellationToken cancellationToken = default);
}

/// <summary>
/// <see cref="IPingChecker"/> backed by ICMP (via <c>System.Net.Sockets.Ping</c>).
/// Uses reflection so the type is only required at runtime, which keeps the
/// library buildable on runtimes where ICMP is not available.
/// </summary>
public class PingChecker : IPingChecker
{
    private static readonly Type? _pingType =
        Type.GetType("System.Net.Sockets.Ping, System.Net.Sockets");

    /// <inheritdoc />
    public async Task<bool> PingAsync(IPAddress address, int timeoutMs, CancellationToken cancellationToken = default)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Only IPv4 addresses can be pinged.", nameof(address));
        }

        if (_pingType is null)
        {
            throw new NotSupportedException(
                "ICMP ping is not available on this runtime; the success check is skipped.");
        }

        object ping = Activator.CreateInstance(_pingType)!;
        MethodInfo send = _pingType.GetMethod(
            "SendAsync",
            new[] { typeof(IPAddress), typeof(int), typeof(CancellationToken) })!;

        Task task = (Task)send.Invoke(ping, new object?[] { address, timeoutMs, cancellationToken })!;
        await task.ConfigureAwait(false);

        object? reply = task.GetType().GetProperty("Result")!.GetValue(task);
        object? status = reply!.GetType().GetProperty("Status")!.GetValue(reply);
        Type ipStatus = _pingType!.Assembly.GetType("System.Net.Sockets.IPStatus")!;
        object success = ipStatus.GetField("Success")!.GetValue(null)!;

        return Equals(status, success);
    }
}
