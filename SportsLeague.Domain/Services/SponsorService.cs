using System.Net.Mail;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services;

public class SponsorService : ISponsorService
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentSponsorRepository _tournamentSponsorRepository;

    public SponsorService(
        ISponsorRepository sponsorRepository,
        ITournamentRepository tournamentRepository,
        ITournamentSponsorRepository tournamentSponsorRepository)
    {
        _sponsorRepository = sponsorRepository;
        _tournamentRepository = tournamentRepository;
        _tournamentSponsorRepository = tournamentSponsorRepository;
    }

    public async Task<IEnumerable<Sponsor>> GetAllAsync()
    {
        return await _sponsorRepository.GetAllAsync();
    }

    public async Task<Sponsor?> GetByIdAsync(int id)
    {
        return await _sponsorRepository.GetByIdAsync(id);
    }

    public async Task<Sponsor> CreateAsync(Sponsor sponsor)
    {
        var existing = await _sponsorRepository.GetByNameAsync(sponsor.Name);
        if (existing != null)
            throw new InvalidOperationException(
                $"Ya existe un sponsor con el nombre '{sponsor.Name}'");

        ValidateEmail(sponsor.ContactEmail);

        return await _sponsorRepository.CreateAsync(sponsor);
    }

    public async Task UpdateAsync(int id, Sponsor sponsor)
    {
        var existingSponsor = await _sponsorRepository.GetByIdAsync(id);
        if (existingSponsor == null)
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {id}");

        if (!string.Equals(existingSponsor.Name, sponsor.Name, StringComparison.OrdinalIgnoreCase))
        {
            var sponsorWithSameName = await _sponsorRepository.GetByNameAsync(sponsor.Name);
            if (sponsorWithSameName != null && sponsorWithSameName.Id != id)
            {
                throw new InvalidOperationException(
                    $"Ya existe un sponsor con el nombre '{sponsor.Name}'");
            }
        }

        ValidateEmail(sponsor.ContactEmail);

        existingSponsor.Name = sponsor.Name;
        existingSponsor.ContactEmail = sponsor.ContactEmail;
        existingSponsor.Phone = sponsor.Phone;
        existingSponsor.WebsiteUrl = sponsor.WebsiteUrl;
        existingSponsor.Category = sponsor.Category;

        await _sponsorRepository.UpdateAsync(existingSponsor);
    }

    public async Task DeleteAsync(int id)
    {
        var exists = await _sponsorRepository.ExistsAsync(id);
        if (!exists)
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {id}");

        await _sponsorRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<TournamentSponsor>> GetTournamentsBySponsorAsync(int sponsorId)
    {
        var sponsorExists = await _sponsorRepository.ExistsAsync(sponsorId);
        if (!sponsorExists)
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {sponsorId}");

        return await _tournamentSponsorRepository.GetBySponsorAsync(sponsorId);
    }

    public async Task<TournamentSponsor> LinkToTournamentAsync(
        int sponsorId,
        int tournamentId,
        decimal contractAmount)
    {
        var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
        if (sponsor == null)
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {sponsorId}");

        var tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament == null)
            throw new KeyNotFoundException(
                $"No se encontró el torneo con ID {tournamentId}");

        if (contractAmount <= 0)
            throw new InvalidOperationException(
                "ContractAmount debe ser mayor a 0");

        var existingLink = await _tournamentSponsorRepository
            .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);

        if (existingLink != null)
            throw new InvalidOperationException(
                "Este sponsor ya está vinculado a este torneo");

        var link = new TournamentSponsor
        {
            SponsorId = sponsorId,
            TournamentId = tournamentId,
            ContractAmount = contractAmount,
            JoinedAt = DateTime.UtcNow
        };

        return await _tournamentSponsorRepository.CreateAsync(link);
    }

    public async Task UnlinkFromTournamentAsync(int sponsorId, int tournamentId)
    {
        var sponsorExists = await _sponsorRepository.ExistsAsync(sponsorId);
        if (!sponsorExists)
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {sponsorId}");

        var tournamentExists = await _tournamentRepository.ExistsAsync(tournamentId);
        if (!tournamentExists)
            throw new KeyNotFoundException(
                $"No se encontró el torneo con ID {tournamentId}");

        var existingLink = await _tournamentSponsorRepository
            .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);

        if (existingLink == null)
            throw new KeyNotFoundException(
                "No existe la vinculación entre el sponsor y el torneo");

        await _tournamentSponsorRepository.DeleteAsync(existingLink.Id);
    }

    private static void ValidateEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
        }
        catch
        {
            throw new InvalidOperationException(
                "ContactEmail no tiene un formato válido");
        }
    }
}