using Domain.Entities;

namespace Application.Common.Interfaces.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}