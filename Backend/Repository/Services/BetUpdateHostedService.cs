using Backend.Repository.Interfaces;
using Backend.Services.Interfaces;

public class BetUpdateHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public BetUpdateHostedService(IServiceProvider serviceProvider)
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
                    var matchService = scope.ServiceProvider.GetRequiredService<IBetService>();
                    await matchService.AutoUpdateBetStatusAsync(); // Call the method
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
