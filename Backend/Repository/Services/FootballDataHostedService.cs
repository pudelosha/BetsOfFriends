using Backend.Model.Database;
using Backend.Model.Entities;
using Backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class FootballDataHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FootballDataHostedService> _logger;

    private readonly TimeSpan _inPlayInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _tournamentCheckInterval = TimeSpan.FromHours(1);

    private DateTime _lastTournamentCheck = DateTime.MinValue;

    public FootballDataHostedService(IServiceProvider serviceProvider, ILogger<FootballDataHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var footballDataService = scope.ServiceProvider.GetRequiredService<IFootballDataService>();

                // Always run every minute
                await CheckTournamentChangesAsync(dbContext, footballDataService, stoppingToken);

                // Run hourly
                if (DateTime.UtcNow - _lastTournamentCheck >= _tournamentCheckInterval)
                {
                    //TODO add if required
                    _lastTournamentCheck = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FootballDataHostedService.");
            }

            await Task.Delay(_inPlayInterval, stoppingToken);
        }
    }

    private async Task CheckTournamentChangesAsync(AppDbContext dbContext, IFootballDataService footballDataService, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking predefined tournament changes...");

        var predefinedTournaments = await dbContext.PredefinedTournaments
            .Where(t => t.ExternalTournamentId != null && t.Update == CustomTournament.TournamentUpdate.Auto)
            .ToListAsync(cancellationToken);

        foreach (var tournament in predefinedTournaments)
        {
            try
            {
                var externalId = tournament.ExternalTournamentId.Value;
                var season = tournament.Season ?? 2024;

                var rawJson = await footballDataService.GetCompetitionMatchesAsync(externalId, season);
                var updatedDto = await footballDataService.ConvertToPredefinedTournamentDtoAsync(rawJson);

                foreach (var updatedMatch in updatedDto.Matches)
                {
                    if (updatedMatch.ExternalMatchId == null)
                        continue;

                    var existingMatch = await dbContext.PredefinedMatches
                        .FirstOrDefaultAsync(m => m.ExternalMatchId == updatedMatch.ExternalMatchId, cancellationToken);

                    if (existingMatch == null)
                        continue;

                    bool hasChanges =
                        existingMatch.Status.ToString() != updatedMatch.MatchStatus ||
                        existingMatch.MatchStart != updatedMatch.MatchStart ||
                        existingMatch.HomeScore != updatedMatch.ScoreHome ||
                        existingMatch.AwayScore != updatedMatch.ScoreAway;

                    if (hasChanges)
                    {
                        _logger.LogInformation($"Updating match {existingMatch.MatchId} based on external match {updatedMatch.ExternalMatchId}");

                        existingMatch.Status = Enum.TryParse(updatedMatch.MatchStatus, out CustomMatch.MatchStatus parsedStatus)
                            ? parsedStatus
                            : existingMatch.Status;

                        existingMatch.MatchStart = DateTime.SpecifyKind(updatedMatch.MatchStart, DateTimeKind.Utc);
                        existingMatch.HomeScore = updatedMatch.ScoreHome;
                        existingMatch.AwayScore = updatedMatch.ScoreAway;

                        // Update all custom matches referencing this predefined match
                        var relatedCustomMatches = await dbContext.CustomMatches
                            .Where(m => m.PredefinedMatchId == existingMatch.MatchId &&
                                        m.Tournament.Update == CustomTournament.TournamentUpdate.Auto)
                            .Include(m => m.Tournament)
                            .ToListAsync(cancellationToken);

                        foreach (var customMatch in relatedCustomMatches)
                        {
                            _logger.LogInformation($"Propagating update to custom match {customMatch.MatchId}");

                            customMatch.MatchStart = existingMatch.MatchStart;
                            customMatch.HomeScore = existingMatch.HomeScore;
                            customMatch.AwayScore = existingMatch.AwayScore;
                            customMatch.Status = existingMatch.Status;
                        }
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing tournament ID {tournament.TournamentId}");
            }
        }

        _logger.LogInformation("Finished checking tournament changes.");
    }
}
