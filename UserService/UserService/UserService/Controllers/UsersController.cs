using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UserService.Dtos;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Users")]
public sealed class UsersController
{

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
    Summary = "Получить пользователя по идентификатору",
    Description = "Получает пользователя по идентификатору",
    Tags = new[] { "getbyid" })]
    [SwaggerResponse(StatusCodes.Status200OK, "Пользователь успешно получен", typeof(UserResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации")]
    public async Task<ActionResult<UserResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [HttpGet]
    [SwaggerOperation(
    Summary = "Получить всех пользователей",
    Description = "Получает всех пользователей",
    Tags = new[] { "getall" })]
    [SwaggerResponse(StatusCodes.Status200OK, "Пользователи успешно получены", typeof(List<UserResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации")]
    public async Task<ActionResult<List<UserResponse>>> GetAll(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Создать пользователя",
        Description = "Создаёт нового пользователя",
        Tags = new[] { "create" })]
    [SwaggerResponse(StatusCodes.Status201Created, "Пользователь успешно создан", typeof(UserResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации")]
    public async Task<ActionResult<UserResponse>> Create(/*[FromBody] CreateUserRequest request,*/ CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Обновить пользователя",
        Description = "Обновляет данные существующего пользователя",
        Tags = new[] { "update" })]
    [SwaggerResponse(StatusCodes.Status200OK, "Пользователь успешно обновлён", typeof(UserResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка валидации")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Пользователь не найден")]
    public async Task<ActionResult<UserResponse>> Update([FromRoute] Guid id,/* [FromBody] UpdateUserRequest request,*/ CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Удалить пользователя",
        Description = "Удаляет пользователя по идентификатору",
        Tags = new[] { "delete" })]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Пользователь успешно удалён")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Пользователь не найден")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
