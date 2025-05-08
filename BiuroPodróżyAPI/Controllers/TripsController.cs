using Microsoft.AspNetCore.Mvc;           
using Microsoft.Data.SqlClient;           
using System.Data;                        
using System.Threading.Tasks;             
using System.Collections.Generic;
using BiuroPodróżyAPI;

namespace BiuroPodróżyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly string _connectionString;

    public TripsController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TravelDb");
    }
//Pobiera wszystkie dostępne wycieczki w bazie danych.
    [HttpGet]
    public async Task<IActionResult> GetTrips()
    {
        var trips = new List<Trip>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            //Pobiera wszystkie wycieczki z tabeli Trips
            var query = @"SELECT t.Id, t.Name, t.Description, t.StartDate, t.EndDate, t.MaxParticipants
                      FROM Trips t";
            using (var command = new SqlCommand(query, connection))
            {
                var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var trip = new Trip
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        StartDate = reader.GetDateTime(3),
                        EndDate = reader.GetDateTime(4),
                        MaxParticipants = reader.GetInt32(5),
                        Countries = new List<Country>()
                    };

                    // Pobiera kraje związane z wycieczką
                    var countryQuery =
                        "SELECT c.Id, c.Name FROM Countries c JOIN Country_Trip ct ON ct.CountryId = c.Id WHERE ct.TripId = @TripId";
                    using (var countryCommand = new SqlCommand(countryQuery, connection))
                    {
                        countryCommand.Parameters.AddWithValue("@TripId", trip.Id);
                        var countryReader = await countryCommand.ExecuteReaderAsync();
                        while (await countryReader.ReadAsync())
                        {
                            trip.Countries.Add(new Country
                            {
                                Id = countryReader.GetInt32(0),
                                Name = countryReader.GetString(1)
                            });
                        }
                    }

                    trips.Add(trip);
                }
            }
        }

        return Ok(trips);
    }
}
