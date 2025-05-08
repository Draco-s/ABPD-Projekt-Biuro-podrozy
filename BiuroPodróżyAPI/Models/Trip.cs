namespace BiuroPodróżyAPI;

public class Trip
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxParticipants { get; set; }
    public List<Country> Countries { get; set; }  
}