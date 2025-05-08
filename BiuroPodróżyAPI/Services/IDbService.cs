namespace BiuroPodróżyAPI.Services;

public interface IDbService
{
    Task<List<Trip>> GetTripsAsync();
    Task<Trip> GetTripByIdAsync(int tripId);
    Task<List<Trip>> GetClientTripsAsync(int clientId);
    Task CreateClientAsync(Client client);
    Task RegisterClientForTripAsync(int clientId, int tripId);
    Task UnregisterClientFromTripAsync(int clientId, int tripId); 
}