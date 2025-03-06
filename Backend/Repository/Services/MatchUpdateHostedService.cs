using Backend.Repository.Interfaces;

public class MatchUpdateHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public MatchUpdateHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope()) // Create a scoped instance
                {
                    var matchService = scope.ServiceProvider.GetRequiredService<IMatchService>();
                    await matchService.AutoUpdateMatchStatusAsync(); // Call the method
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MatchUpdateHostedService: {ex.Message}");
            }

            await Task.Delay(_interval, stoppingToken); // Wait before next execution
        }
    }
}
