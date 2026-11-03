namespace UserService.Dtos;

public class UpdateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Manager или Admin.
    /// </summary>
    public string Role { get; set; } = "Manager";

    public bool IsActive { get; set; } = true;
}
