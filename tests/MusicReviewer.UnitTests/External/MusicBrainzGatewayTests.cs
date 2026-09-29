using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Infrastructure.MusicBrainz;

namespace MusicReviewer.UnitTests.External;

/// <summary>
/// The gateway waits on a fake clock. Tests advance that clock in small steps until the expected
/// request goes out, then assert on the recorded (fake) send times, so they don't depend on
/// how the worker thread is scheduled relative to the test thread.
/// </summary>
public sealed class MusicBrainzGatewayTests : IAsyncLifetime
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Pause = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(100);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly ConcurrentQueue<(string Name, DateTimeOffset At)> _sent = new();
    private readonly MusicBrainzGateway _gateway;

    public MusicBrainzGatewayTests()
    {
        var options = Options.Create(new MusicBrainzOptions { MinInterval = Interval, PauseAfterUnavailable = Pause });
        _gateway = new MusicBrainzGateway(options, _clock, NullLogger<MusicBrainzGateway>.Instance);
    }

    public async ValueTask InitializeAsync() => await _gateway.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _gateway.StopAsync(CancellationToken.None);
        _gateway.Dispose();
    }

    private Task<HttpResponseMessage> Send(string name, RequestPriority priority = RequestPriority.Background, HttpStatusCode status = HttpStatusCode.OK, CancellationToken? cancellationToken = null) =>
        _gateway.SendAsync(_ =>
        {
            _sent.Enqueue((name, _clock.GetUtcNow()));
            return Task.FromResult(new HttpResponseMessage(status));
        }, priority, cancellationToken ?? TestContext.Current.CancellationToken);

    /// <summary>Waits (in real time) without moving the clock, for requests that need no delay.</summary>
    private async Task WaitForSentAsync(int count)
    {
        for (var i = 0; i < 200 && _sent.Count < count; i++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.Equal(count, _sent.Count);
    }

    /// <summary>Moves the fake clock forward step by step until <paramref name="count"/> requests have gone out.</summary>
    private async Task AdvanceUntilSentAsync(int count)
    {
        for (var i = 0; i < 400 && _sent.Count < count; i++)
        {
            _clock.Advance(Step);
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
        Assert.Equal(count, _sent.Count);
    }

    /// <summary>Asserts nothing more is sent while real time passes and the clock stays put.</summary>
    private async Task AssertHeldBackAsync(int count)
    {
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.Equal(count, _sent.Count);
    }

    private List<DateTimeOffset> SendTimes => [.. _sent.Select(s => s.At)];

    [Fact]
    public async Task Sends_at_most_one_request_per_interval()
    {
        var tasks = new[] { Send("a"), Send("b"), Send("c") };

        await WaitForSentAsync(1);
        await AssertHeldBackAsync(1);
        await AdvanceUntilSentAsync(3);
        await Task.WhenAll(tasks);

        var times = SendTimes;
        Assert.True(times[1] - times[0] >= Interval, $"gap was {times[1] - times[0]}");
        Assert.True(times[2] - times[1] >= Interval, $"gap was {times[2] - times[1]}");
    }

    [Fact]
    public async Task Serves_interactive_requests_before_queued_background_ones()
    {
        var first = Send("first");
        await WaitForSentAsync(1);

        var background = Send("background");
        var interactive = Send("interactive", RequestPriority.Interactive);
        await AssertHeldBackAsync(1);

        await AdvanceUntilSentAsync(3);
        await Task.WhenAll(first, background, interactive);

        Assert.Equal(["first", "interactive", "background"], _sent.Select(s => s.Name));
    }

    [Fact]
    public async Task Pauses_all_traffic_after_a_503()
    {
        var rejected = Send("rejected", status: HttpStatusCode.ServiceUnavailable);
        await WaitForSentAsync(1);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await rejected).StatusCode);

        var next = Send("next");
        await AdvanceUntilSentAsync(2);
        await next;

        // The normal 1s interval was not enough: the whole gateway paused for the 503 back-off.
        var times = SendTimes;
        Assert.True(times[1] - times[0] >= Pause, $"gap was {times[1] - times[0]}");
    }

    [Fact]
    public async Task Drops_requests_cancelled_while_waiting()
    {
        var first = Send("first");
        await WaitForSentAsync(1);

        using var cts = new CancellationTokenSource();
        var cancelled = Send("cancelled", cancellationToken: cts.Token);
        var kept = Send("kept");
        await cts.CancelAsync();

        await AdvanceUntilSentAsync(2);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        await Task.WhenAll(first, kept);

        Assert.Equal(["first", "kept"], _sent.Select(s => s.Name));
    }
}
