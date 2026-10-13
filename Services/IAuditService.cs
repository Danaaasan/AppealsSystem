using System.Security.Claims;

namespace AppealsSystem.Services
{
    public interface IAuditService
    {
        Task LogAsync(ClaimsPrincipal user, string action, string entity, int entityId);
    }
}