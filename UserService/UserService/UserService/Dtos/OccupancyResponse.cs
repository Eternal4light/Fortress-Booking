namespace UserService.Dtos;

/// <summary>
/// Сколько мест коворкинга занято приглашениями (в ожидании ответа и принятыми).
/// </summary>
public class OccupancyResponse
{
    public int OccupiedSeats { get; set; }
}
