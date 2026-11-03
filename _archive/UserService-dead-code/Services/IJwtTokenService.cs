using UserService.Domain;

namespace UserService.Services;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
