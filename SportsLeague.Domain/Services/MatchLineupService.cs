using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services;

public class MatchLineupService : IMatchLineupService
{
    private readonly IMatchLineupRepository _matchLineupRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IPlayerRepository _playerRepository;

    public MatchLineupService(
        IMatchLineupRepository matchLineupRepository,
        IMatchRepository matchRepository,
        IPlayerRepository playerRepository)
    {
        _matchLineupRepository = matchLineupRepository;
        _matchRepository = matchRepository;
        _playerRepository = playerRepository;
    }

    public async Task<MatchLineup> AddPlayerAsync(int matchId, MatchLineup lineup)
    {
        var match = await _matchRepository.GetByIdWithDetailsAsync(matchId)
            ?? throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        var player = await _playerRepository.GetByIdWithTeamAsync(lineup.PlayerId)
            ?? throw new KeyNotFoundException($"No se encontró el jugador con ID {lineup.PlayerId}");

        if (match.Status != MatchStatus.Scheduled)
            throw new InvalidOperationException("Solo se pueden registrar alineaciones en partidos Scheduled");

        var belongsToMatch =
            player.TeamId == match.HomeTeamId || player.TeamId == match.AwayTeamId;

        if (!belongsToMatch)
            throw new InvalidOperationException("El jugador no pertenece a ninguno de los equipos del partido");

        var exists = await _matchLineupRepository.ExistsByMatchAndPlayerAsync(matchId, lineup.PlayerId);
        if (exists)
            throw new InvalidOperationException("El jugador ya está registrado en la alineación de este partido");

        if (lineup.IsStarter)
        {
            var starters = await _matchLineupRepository.CountStartersByMatchAndTeamAsync(matchId, player.TeamId);
            if (starters >= 11)
                throw new InvalidOperationException("El equipo ya tiene 11 titulares registrados en este partido");
        }

        lineup.MatchId = matchId;
        return await _matchLineupRepository.CreateAsync(lineup);
    }

    public async Task<IEnumerable<MatchLineup>> GetByMatchAsync(int matchId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId)
            ?? throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        return await _matchLineupRepository.GetByMatchAsync(matchId);
    }

    public async Task<IEnumerable<MatchLineup>> GetByMatchAndTeamAsync(int matchId, int teamId)
    {
        var match = await _matchRepository.GetByIdWithDetailsAsync(matchId)
            ?? throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        if (teamId != match.HomeTeamId && teamId != match.AwayTeamId)
            throw new InvalidOperationException("El equipo no pertenece al partido indicado");

        return await _matchLineupRepository.GetByMatchAndTeamAsync(matchId, teamId);
    }

    public async Task DeleteAsync(int matchId, int lineupId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId)
            ?? throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");

        var lineup = await _matchLineupRepository.GetByIdAsync(lineupId);
        if (lineup == null || lineup.MatchId != matchId)
            throw new KeyNotFoundException("No se encontró el registro de alineación");

        await _matchLineupRepository.DeleteAsync(lineupId);
    }
}
