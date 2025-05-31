using EMSApp.Application;

public class LeaveCompletionOverdueTaskService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LeaveCompletionOverdueTaskService(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CompleteLeaves(stoppingToken);

        var now = DateTimeOffset.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        var initialDelay = nextMidnight - now;
        if (initialDelay < TimeSpan.Zero) initialDelay = TimeSpan.Zero;
        if (initialDelay.TotalMilliseconds > int.MaxValue)
            initialDelay = TimeSpan.FromDays(1);

        await Task.Delay(initialDelay, stoppingToken);

        var daily = TimeSpan.FromDays(1);
        while (!stoppingToken.IsCancellationRequested)
        {
            await CompleteLeaves(stoppingToken);

            await Task.Delay(daily, stoppingToken);
        }
    }

    private async Task CompleteLeaves(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ILeaveRequestService>();
        await svc.CompleteDueRequestsAsync(ct);
    }
}