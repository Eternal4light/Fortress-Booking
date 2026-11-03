using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Domain;
using UserService.Dtos;
using UserService.Repositories;
using UserService.Services.Keycloak;

namespace UserService.Controllers;

/// <summary>
/// Управление сотрудниками (менеджерами и администраторами) — доступно только администратору.
/// Учётные записи и пароли сотрудников живут в Keycloak, здесь хранится только профиль.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Employees")]
[Authorize(Roles = "Admin")]
public sealed class EmployeesController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IKeycloakClient _keycloak;

    public EmployeesController(IUserRepository userRepository, IKeycloakClient keycloak)
    {
        _userRepository = userRepository;
        _keycloak = keycloak;
    }

    private Guid? CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Список сотрудников (менеджеров и администраторов)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll(CancellationToken ct)
    {
        var managers = await _userRepository.GetAllAsync(UserRole.Manager);
        var admins = await _userRepository.GetAllAsync(UserRole.Admin);

        var response = managers.Concat(admins)
            .OrderBy(u => u.LastName)
            .Select(u => u.ToResponse())
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Создать нового сотрудника
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role) || role == UserRole.Client)
        {
            return BadRequest("Роль сотрудника должна быть Manager или Admin");
        }

        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict("Пользователь с таким email уже существует");
        }

        Guid keycloakId;
        try
        {
            keycloakId = await _keycloak.CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, role);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "Не удалось создать пользователя в Keycloak", detail: ex.Message, statusCode: 502);
        }
        catch (HttpRequestException ex)
        {
            return Problem(title: "Keycloak недоступен", detail: ex.Message, statusCode: 503);
        }

        var user = new User
        {
            Id = keycloakId,
            FirstName = request.FirstName,
            SecondName = request.SecondName,
            LastName = request.LastName,
            Phone = request.Phone,
            Email = request.Email,
            Role = role,
            IsActive = true
        };

        await _userRepository.AddAsync(user);

        return Created($"api/employees/{user.Id}", user.ToResponse());
    }

    /// <summary>
    /// Отредактировать данные существующего сотрудника
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> Update([FromRoute] Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null || user.Role == UserRole.Client)
        {
            return NotFound();
        }

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role) || role == UserRole.Client)
        {
            return BadRequest("Роль сотрудника должна быть Manager или Admin");
        }

        if (role != user.Role)
        {
            await _keycloak.UpdateRoleAsync(user.Id, user.Role, role);
        }

        if (request.IsActive != user.IsActive)
        {
            await _keycloak.SetEnabledAsync(user.Id, request.IsActive);
        }

        user.FirstName = request.FirstName;
        user.SecondName = request.SecondName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Role = role;
        user.IsActive = request.IsActive;

        await _userRepository.UpdateAsync(user);

        return Ok(user.ToResponse());
    }

    /// <summary>
    /// Удалить сотрудника
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        if (CurrentUserId == id)
        {
            return BadRequest("Нельзя удалить свою же учётную запись");
        }

        var user = await _userRepository.GetByIdAsync(id);
        if (user is null || user.Role == UserRole.Client)
        {
            return NotFound();
        }

        await _keycloak.DeleteUserAsync(id);
        await _userRepository.DeleteAsync(id);
        return NoContent();
    }
}
