using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsLeague.API.DTOs.Request;
using SportsLeague.API.DTOs.Response;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SponsorController : ControllerBase
{
    private readonly ISponsorService _sponsorService;
    private readonly IMapper _mapper;
    private readonly ILogger<SponsorController> _logger;

    public SponsorController(
        ISponsorService sponsorService,
        IMapper mapper,
        ILogger<SponsorController> logger)
    {
        _sponsorService = sponsorService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SponsorResponseDTO>>> GetAll()
    {
        var sponsors = await _sponsorService.GetAllAsync();
        var response = _mapper.Map<IEnumerable<SponsorResponseDTO>>(sponsors);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SponsorResponseDTO>> GetById(int id)
    {
        var sponsor = await _sponsorService.GetByIdAsync(id);

        if (sponsor == null)
            return NotFound(new { message = $"No se encontró el sponsor con ID {id}" });

        var response = _mapper.Map<SponsorResponseDTO>(sponsor);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<SponsorResponseDTO>> Create([FromBody] SponsorRequestDTO request)
    {
        try
        {
            var sponsor = _mapper.Map<Sponsor>(request);
            var createdSponsor = await _sponsorService.CreateAsync(sponsor);
            var response = _mapper.Map<SponsorResponseDTO>(createdSponsor);

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation error while creating sponsor");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating sponsor");
            return StatusCode(500, new { message = "Ocurrió un error interno del servidor" });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SponsorRequestDTO request)
    {
        try
        {
            var sponsor = _mapper.Map<Sponsor>(request);
            await _sponsorService.UpdateAsync(id, sponsor);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Sponsor not found while updating");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation error while updating sponsor");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating sponsor");
            return StatusCode(500, new { message = "Ocurrió un error interno del servidor" });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _sponsorService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Sponsor not found while deleting");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation error while deleting sponsor");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting sponsor");
            return StatusCode(500, new { message = "Ocurrió un error interno del servidor" });
        }
    }

    [HttpGet("{id:int}/tournaments")]
    public async Task<ActionResult<IEnumerable<TournamentSponsorResponseDTO>>> GetTournamentsBySponsor(int id)
    {
        try
        {
            var links = await _sponsorService.GetTournamentsBySponsorAsync(id);
            var response = _mapper.Map<IEnumerable<TournamentSponsorResponseDTO>>(links);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Sponsor not found while listing tournaments");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while listing tournaments for sponsor");
            return StatusCode(500, new { message = "Ocurrió un error interno del servidor" });
        }
    }

    [HttpPost("{id:int}/tournaments")]
    public async Task<ActionResult<TournamentSponsorResponseDTO>> LinkToTournament(
        int id,
        [FromBody] TournamentSponsorRequestDTO request)
    {
        try
        {
            var createdLink = await _sponsorService.LinkToTournamentAsync(
                id,
                request.TournamentId,
                request.ContractAmount);

            var response = _mapper.Map<TournamentSponsorResponseDTO>(createdLink);

            return CreatedAtAction(
                nameof(GetTournamentsBySponsor),
                new { id },
                response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found while linking sponsor to tournament");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation error while linking sponsor to tournament");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while linking sponsor to tournament");
            return StatusCode(500, new { message = "Ocurrió un error interno del servidor" });
        }
    }

    [HttpDelete("{id:int}/tournaments/{tournamentId:int}")]
    public async Task<IActionResult> UnlinkFromTournament(int id, int tournamentId)
    {
        try
        {
            await _sponsorService.UnlinkFromTournamentAsync(id, tournamentId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Relationship not found while unlinking sponsor from tournament");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation error while unlinking sponsor from tournament");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while unlinking sponsor from tournament");
            return StatusCode(500, new { message = "Ocurrió un error interno del servidor" });
        }
    }
}