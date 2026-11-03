namespace UserService.Dtos;

public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Деятельность/профессия — по ней клиенту будут приходить приглашения от коворкингов.
    /// </summary>
    public string Activity { get; set; } = string.Empty;
}
