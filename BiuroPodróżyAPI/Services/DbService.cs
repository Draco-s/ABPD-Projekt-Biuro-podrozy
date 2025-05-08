using Microsoft.Data.SqlClient;
using BiuroPodróżyAPI.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace BiuroPodróżyAPI.Services;


public class DbService : IDbService
{
    private readonly string _connectionString;

    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TravelDb");
    }

    public async Task<List<Trip>> GetTripsAsync()
    {
        var trips = new List<Trip>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            //pobranie wszystkich dostępnych wycieczek z tabeli Trips
            var query = @"SELECT t.Id, t.Name, t.Description, t.StartDate, t.EndDate, t.MaxParticipants FROM Trips t";
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

                    // Pobieranie krajów powiązanych z wycieczką
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

        return trips;
    }
     public async Task<Trip> GetTripByIdAsync(int tripId)
    {
        Trip trip = null;

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            //wybieranie danych o wycieczce z tabeli Trips na podstawie jej ID
            var query = @"SELECT t.Id, t.Name, t.Description, t.StartDate, t.EndDate, t.MaxParticipants 
                          FROM Trips t WHERE t.Id = @TripId";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@TripId", tripId);
                var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    trip = new Trip
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        StartDate = reader.GetDateTime(3),
                        EndDate = reader.GetDateTime(4),
                        MaxParticipants = reader.GetInt32(5),
                        Countries = new List<Country>()
                    };

                    // Pobieranie krajów powiązanych z wycieczką
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
                }
            }
        }

        return trip;
    }

    public async Task<List<Trip>> GetClientTripsAsync(int clientId)
    {
        var clientTrips = new List<Trip>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            //pobieranie szczegółów jednej wycieczki na podstawie jej unikalnego Id
            var query = @"SELECT t.Id, t.Name, t.Description, t.StartDate, t.EndDate, t.MaxParticipants
                      FROM Trips t
                      JOIN Client_Trip ct ON ct.TripId = t.Id
                      WHERE ct.ClientId = @ClientId";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ClientId", clientId);
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

                    // Pobieranie krajów związanych z wycieczką
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

                    clientTrips.Add(trip);
                }
            }
        }

        return clientTrips;
    }

    public async Task CreateClientAsync(Client client)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            //wstawianie nowego klienta do tabeli Clients
            var query = @"INSERT INTO Clients (FirstName, LastName, Email, Telephone, Pesel) 
                      OUTPUT INSERTED.Id 
                      VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@FirstName", client.FirstName);
                command.Parameters.AddWithValue("@LastName", client.LastName);
                command.Parameters.AddWithValue("@Email", client.Email);
                command.Parameters.AddWithValue("@Telephone", client.Telephone);
                command.Parameters.AddWithValue("@Pesel", client.Pesel);

                client.Id = (int)await command.ExecuteScalarAsync(); // Pobranie ID nowego klienta
            }
        }
    }

    public async Task RegisterClientForTripAsync(int clientId, int tripId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // Sprawdzenie, czy wycieczka nie osiągnęła maksymalnej liczby uczestników
            var tripQuery =
                "SELECT MaxParticipants, (SELECT COUNT(*) FROM Client_Trip WHERE TripId = @TripId) AS RegisteredCount FROM Trips WHERE Id = @TripId";
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
                        throw new Exception("This trip is fully booked.");
                    }
                }
            }

            // Rejestracja klienta na wycieczkę
            var registerQuery =
                "INSERT INTO Client_Trip (ClientId, TripId, RegisteredAt) VALUES (@ClientId, @TripId, @RegisteredAt)";
            using (var command = new SqlCommand(registerQuery, connection))
            {
                command.Parameters.AddWithValue("@ClientId", clientId);
                command.Parameters.AddWithValue("@TripId", tripId);
                command.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task UnregisterClientFromTripAsync(int clientId, int tripId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            //usuwanie rejestracjcji klienta na wycieczkę z tabeli Client_Trip
            var query = "DELETE FROM Client_Trip WHERE ClientId = @ClientId AND TripId = @TripId";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ClientId", clientId);
                command.Parameters.AddWithValue("@TripId", tripId);
                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("Registration not found.");
                }
            }
        }
    }
}
