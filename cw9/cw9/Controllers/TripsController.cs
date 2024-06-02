using cw9.Data;
using cw9.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cw9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly MasterContext _context;

    public TripsController(MasterContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetTrips(int page = 1, int pageSize = 10)
    {
        var totalTrips = await _context.Trips.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalTrips / pageSize);
        
        var trips = await _context.Trips
            .Select(e => new
            {
                Name = e.Name,
                Description = e.Description,
                DateFrom = e.DateFrom,
                DateTo = e.DateTo,
                MaxPeople = e.MaxPeople,
                Countries = e.IdCountries.Select(c => new
                {
                    Name = c.Name
                }),
                Clients = e.ClientTrips.Select(ct => new
                {
                    FirstName = ct.IdClientNavigation.FirstName,
                    LastName = ct.IdClientNavigation.LastName
                        
                })
            })
            .ToListAsync();

        var result = new
        {
            pageNum = page,
            pageSize,
            allPages = totalPages,
            trips
        };
        
        return Ok(result);
    }

    [HttpDelete("{idClient:int}")]
    public async Task<IActionResult> DeleteClient(int idClient)
    {
        var client = await _context.Clients.FindAsync(idClient);
        if (client == null)
        {
            return NotFound();
        }
        
        var hasTrips = await _context.ClientTrips
            .AnyAsync(ct => ct.IdClient == idClient);

        if (hasTrips)
        {
            return StatusCode(409, "Klient ma przypisane wycieczki i nie może zostać usunięty");
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpPost("{idTrip:int}/clients")]
    public async Task<IActionResult> AssignClientToTrip(int idTrip, ClientDto clientDto)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Pesel == clientDto.Pesel);
        if (client == null)
        {
            return BadRequest("Klient o tym numerze pesel nie istnieje");
        }

        var isAssigned = await _context.ClientTrips.AnyAsync(ct => ct.IdTrip == idTrip && ct.IdClient == client.IdClient);
        if (isAssigned)
        {
            return BadRequest("Klient jest już przypisany do tej wycieczki");
        }

        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.IdTrip == idTrip && t.DateFrom > DateTime.Now);
        if (trip == null)
        {
            return BadRequest("Wycieczka nie istnieje lub już się odbyła");
        }

        var clientTrip = new ClientTrip
        {
            IdClient = client.IdClient,
            IdTrip = idTrip,
            RegisteredAt = DateTime.Now
        };

        _context.ClientTrips.Add(clientTrip);
        await _context.SaveChangesAsync();
        
        return Ok("Klient został przypisany do wycieczki");
    }
    

    public class ClientDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
        public string Pesel { get; set; }
        public DateTime PaymentDate { get; set; }
    }
    
}

/*
 * //get - include 
   var tripsInclude = await _context.Trips
       .Include(e => e.IdCountries)
       .Select(e => new
           { 
               Name = e.Name
           })
       .ToListAsync();
*/