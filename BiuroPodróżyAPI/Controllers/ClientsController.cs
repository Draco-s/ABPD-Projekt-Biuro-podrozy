using Microsoft.AspNetCore.Mvc;           
using Microsoft.Data.SqlClient;           
using System.Data;                        
using System.Threading.Tasks;             
using System.Collections.Generic;
using BiuroPodróżyAPI;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly string _connectionString;

    public ClientsController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TravelDb");
    } 
    // Pobiera wszystkie wycieczki powiązane z klientem o danym ID
    [HttpGet("{id}/trips")]
public async Task<IActionResult> GetClientTrips(int id)
{
    var clientTrips = new List<Trip>();

    using (var connection = new SqlConnection(_connectionString))
    {
        await connection.OpenAsync();
        // Pobiera wszystkie wycieczki powiązane z danym klientem
        var query = @"SELECT t.Id, t.Name, t.Description, t.StartDate, t.EndDate, t.MaxParticipants
                      FROM Trips t
                      JOIN Client_Trip ct ON ct.TripId = t.Id
                      WHERE ct.ClientId = @ClientId";
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@ClientId", id);
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
                //Pobiera kraje związane z wycieczką
                var countryQuery = "SELECT c.Id, c.Name FROM Countries c JOIN Country_Trip ct ON ct.CountryId = c.Id WHERE ct.TripId = @TripId";
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

                clientTrips.Add(trip);
            }
        }
    }

    return Ok(clientTrips);
}
//Tworzy nowego klienta w bazie danych.
    [HttpPost]
    public async Task<IActionResult> CreateClient(Client client)
    {
        if (client == null || string.IsNullOrEmpty(client.FirstName) || string.IsNullOrEmpty(client.LastName))
        {
            return BadRequest("Invalid client data.");
        }

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            //Wstawienie nowego klienta do tabeli Clients
            var query =
                "INSERT INTO Clients (FirstName, LastName, Email, Telephone, Pesel) OUTPUT INSERTED.Id VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@FirstName", client.FirstName);
                command.Parameters.AddWithValue("@LastName", client.LastName);
                command.Parameters.AddWithValue("@Email", client.Email);
                command.Parameters.AddWithValue("@Telephone", client.Telephone);
                command.Parameters.AddWithValue("@Pesel", client.Pesel);

                var newClientId = (int)await command.ExecuteScalarAsync();
                client.Id = newClientId;
            }
        }

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }
    //Rejestruje klienta na wycieczkę o danym ID.
    [HttpPut("{id}/trips/{tripId}")]
public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
{
    using (var connection = new SqlConnection(_connectionString))
    {
        await connection.OpenAsync();

        //Sprawdzanie, czy wycieczka istnieje oraz ile osób jest zarejestrowanych
        var tripQuery = "SELECT MaxParticipants, (SELECT COUNT(*) FROM Client_Trip WHERE TripId = @TripId) AS RegisteredCount FROM Trips WHERE Id = @TripId";
        using (var command = new SqlCommand(tripQuery, connection))
        {
            command.Parameters.AddWithValue("@TripId", tripId);
            var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int maxParticipants = reader.GetInt32(0);
                int registeredCount = reader.GetInt32(1);

                if (registeredCount >= maxParticipants)
                {
                    return BadRequest("This trip is fully booked.");
                }
            }
            else
            {
                return NotFound("Trip not found.");
            }
        }

        //  Sprawdzamy, czy klient jest już zarejestrowany na tę wycieczkę
        var registrationQuery = "SELECT COUNT(*) FROM Client_Trip WHERE ClientId = @ClientId AND TripId = @TripId";
        using (var command = new SqlCommand(registrationQuery, connection))
        {
            command.Parameters.AddWithValue("@ClientId", id);
            command.Parameters.AddWithValue("@TripId", tripId);
            var isRegistered = (int)await command.ExecuteScalarAsync();
            if (isRegistered > 0)
            {
                return BadRequest("Client is already registered for this trip.");
            }
        }

        //Rejestrujemy klienta na wycieczkę
        var registerQuery = "INSERT INTO Client_Trip (ClientId, TripId, RegisteredAt) VALUES (@ClientId, @TripId, @RegisteredAt)";
        using (var command = new SqlCommand(registerQuery, connection))
        {
            command.Parameters.AddWithValue("@ClientId", id);
            command.Parameters.AddWithValue("@TripId", tripId);
            command.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);
            await command.ExecuteNonQueryAsync();
        }
    }

    return Ok("Client registered for trip.");
}
//Wyrejestrowuje klienta z wycieczki o danym ID.
    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            //Usuwamy rejestrację klienta z wycieczki
            var query = "DELETE FROM Client_Trip WHERE ClientId = @ClientId AND TripId = @TripId";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ClientId", id);
                command.Parameters.AddWithValue("@TripId", tripId);
                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound("Registration not found.");
                }
            }
        }

        return Ok("Client unregistered from trip.");
    }
    //Pobiera dane klienta na podstawie jego ID.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetClient(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        //Pobiera dane klienta na podstawie jego ID
        var query = "SELECT Id, FirstName, LastName, Email, Telephone, Pesel FROM Clients WHERE Id = @Id";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var client = new Client
            {
                Id = reader.GetInt32(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Email = reader.GetString(3),
                Telephone = reader.GetString(4),
                Pesel = reader.GetString(5)
            };
            return Ok(client);
        }

        return NotFound();
    }
}