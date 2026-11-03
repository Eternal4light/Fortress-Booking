namespace UserService.Dtos;

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Manager или Admin.
    /// </summary>
    public string Role { get; set; } = "Manager";
}
