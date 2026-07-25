using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UserService.Dtos;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("CurrentUser")]
public sealed class CurrentUserController
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Получить текущего пользователя",
        Description = "Возвращает данные аутентифицированного пользователя из сервиса аутентификации",
        Tags = new[] { "getcurrentuser" })]
    [SwaggerResponse(StatusCodes.Status200OK, "Текущий пользователь успешно получен", typeof(UserResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не аутентифицирован")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
