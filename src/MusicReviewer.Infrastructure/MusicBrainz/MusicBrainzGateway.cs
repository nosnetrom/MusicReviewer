using System.Net;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicReviewer.Application.Abstractions;

namespace MusicReviewer.Infrastructure.MusicBrainz;

/// <summary>
/// The single worker through which every MusicBrainz request passes. It sends one request at a
/// time, spaced by <see cref="MusicBrainzOptions.MinInterval"/>, serving interactive requests before
/// background ones. MusicBrainz rejects all traffic from an IP that exceeds 1 request/sec, so this
/// must be the only path to it (see docs/PLAN.md, decision 14).
/// </summary>
public sealed partial class MusicBrainzGateway(
    IOptions<MusicBrainzOptions> options,
    TimeProvider clock,
    ILogger<MusicBrainzGateway> logger) : BackgroundService
{
    private readonly Channel<WorkItem> _interactive = Channel.CreateUnbounded<WorkItem>();
    private readonly Channel<WorkItem> _background = Channel.CreateUnbounded<WorkItem>();
    private readonly SemaphoreSlim _pending = new(0);
    private DateTimeOffset _nextSlot = DateTimeOffset.MinValue;

    public Task<HttpResponseMessage> SendAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        RequestPriority priority,
        CancellationToken cancellationToken)
    {
        var item = new WorkItem(send, cancellationToken);
        var channel = priority == RequestPriority.Interactive ? _interactive : _background;
        if (!channel.Writer.TryWrite(item))
            throw new ExternalServiceUnavailableException("MusicBrainz");

        _pending.Release();
        return item.Completion.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _pending.WaitAsync(stoppingToken);
                if (!_interactive.Reader.TryRead(out var item) && !_background.Reader.TryRead(out item))
                    continue;

                if (item.CancellationToken.IsCancellationRequested)
                {
                    item.Completion.TrySetCanceled(item.CancellationToken);
                    continue;
                }

                var wait = _nextSlot - clock.GetUtcNow();
                if (wait > TimeSpan.Zero)
                    await Task.Delay(wait, clock, stoppingToken);

                _nextSlot = clock.GetUtcNow() + options.Value.MinInterval;
                await RunAsync(item, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            DrainOnShutdown();
        }
    }

    private async Task RunAsync(WorkItem item, CancellationToken stoppingToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(item.CancellationToken, stoppingToken);
        try
        {
            var response = await item.Send(linked.Token);
            if (response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests)
            {
                // Back off for everyone, not just this caller: the limit is per IP.
                _nextSlot = clock.GetUtcNow() + options.Value.PauseAfterUnavailable;
                LogUnavailable(logger, (int)response.StatusCode, options.Value.PauseAfterUnavailable);
            }

            item.Completion.TrySetResult(response);
        }
        catch (OperationCanceledException) when (item.CancellationToken.IsCancellationRequested)
        {
            item.Completion.TrySetCanceled(item.CancellationToken);
        }
        catch (Exception ex)
        {
            item.Completion.TrySetException(ex);
        }
    }

    private void DrainOnShutdown()
    {
        while (_interactive.Reader.TryRead(out var item) || _background.Reader.TryRead(out item))
            item.Completion.TrySetCanceled();
    }

    public override void Dispose()
    {
        _pending.Dispose();
        base.Dispose();
    }

    private sealed record WorkItem(Func<CancellationToken, Task<HttpResponseMessage>> Send, CancellationToken CancellationToken)
    {
        public TaskCompletionSource<HttpResponseMessage> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "MusicBrainz returned {Status}; pausing all requests for {Pause}")]
    private static partial void LogUnavailable(ILogger logger, int status, TimeSpan pause);
}

/// <summary>Routes each MusicBrainz HTTP request through the <see cref="MusicBrainzGateway"/>.</summary>
public sealed class MusicBrainzGatewayHandler(MusicBrainzGateway gateway) : DelegatingHandler
{
    public static readonly HttpRequestOptionsKey<RequestPriority> PriorityKey = new("MusicBrainz.Priority");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var priority = request.Options.TryGetValue(PriorityKey, out var p) ? p : RequestPriority.Background;
        return gateway.SendAsync(ct => base.SendAsync(request, ct), priority, cancellationToken);
    }
}
