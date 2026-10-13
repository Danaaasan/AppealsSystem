using AppealsSystem.Data;
using AppealsSystem.Models;
using System.Security.Claims;

namespace AppealsSystem.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(ClaimsPrincipal user, string action, string entity, int entityId)
        {
            var log = new AuditLog
            {
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                UserName = user.Identity?.Name ?? "anonymous",
                Action = action,
                EntityName = entity,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}