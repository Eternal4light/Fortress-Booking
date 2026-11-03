namespace UserService.Dtos;

public class UpdateCurrentUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Заполняется только у клиентов — менеджер/админ деятельность не указывают.
    /// </summary>
    public string? Activity { get; set; }
}
