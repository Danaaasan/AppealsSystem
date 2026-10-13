using AppealsSystem.Data;
using AppealsSystem.Models;
using AppealsSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppealsSystem.Controllers
{
    [Authorize]
    public class AppealsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryption;
        private readonly IAuditService _audit;
        private readonly UserManager<IdentityUser> _userManager;

        public AppealsController(
            ApplicationDbContext context,
            IEncryptionService encryption,
            IAuditService audit,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _encryption = encryption;
            _audit = audit;
            _userManager = userManager;
        }

        // ==================== ДАШБОРД ====================
        [Authorize(Roles = "Admin,Specialist")]
        public async Task<IActionResult> Dashboard()
        {
            var appeals = await _context.Appeals.ToListAsync();
            ViewBag.Total = appeals.Count;
            ViewBag.New = appeals.Count(a => a.Status == "Зарегистрировано");
            ViewBag.InProgress = appeals.Count(a => a.Status == "На исполнении");
            ViewBag.Overdue = appeals.Count(a => a.Deadline < DateTime.Now && a.Status != "Закрыто");
            ViewBag.Closed = appeals.Count(a => a.Status == "Закрыто");
            return View();
        }

        // ==================== СПИСОК ====================
        [Authorize(Roles = "Admin,Specialist,Executor")]
        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.Appeals
                .Include(a => a.Citizen)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            var appeals = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(appeals);
        }

        // ==================== МОИ ОБРАЩЕНИЯ (гражданин) ====================
        [Authorize(Roles = "Citizen")]
        public async Task<IActionResult> My()
        {
            var email = User.Identity?.Name;
            var appeals = await _context.Appeals
                .Include(a => a.Citizen)
                .Where(a => a.Citizen != null && a.Citizen.Email == email)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(appeals);
        }

        // ==================== ДЕТАЛИ ====================
        public async Task<IActionResult> Details(int id)
        {
            var appeal = await _context.Appeals
                .Include(a => a.Citizen)
                .Include(a => a.Responses)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appeal == null) return NotFound();

            // Если гражданин — проверяем, что это его обращение
            if (User.IsInRole("Citizen"))
            {
                var email = User.Identity?.Name;
                if (appeal.Citizen?.Email != email)
                    return Forbid();
            }

            if (User.IsInRole("Admin"))
            {
                await _audit.LogAsync(User, "ViewPD", "Citizen", appeal.CitizenId);
                if (appeal.Citizen?.EncryptedPassport != null)
                    appeal.Citizen.EncryptedPassport = _encryption.Decrypt(appeal.Citizen.EncryptedPassport);
            }
            else if (appeal.Citizen != null)
            {
                appeal.Citizen.EncryptedPassport = "**** ******";
                appeal.Citizen.Phone = "***";
            }

            // Список исполнителей для формы назначения
            if (User.IsInRole("Admin") || User.IsInRole("Specialist"))
            {
                ViewBag.Executors = await _userManager.GetUsersInRoleAsync("Executor");
            }

            return View(appeal);
        }

        // ==================== СОЗДАНИЕ (гражданин) ====================
        [Authorize(Roles = "Citizen")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Citizen")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appeal appeal)
        {
            ModelState.Remove("Citizen");
            if (ModelState.IsValid)
            {
                var email = User.Identity?.Name ?? "unknown";
                var citizen = await _context.Citizens.FirstOrDefaultAsync(c => c.Email == email);
                if (citizen == null)
                {
                    citizen = new Citizen { FullName = email, Email = email };
                    _context.Citizens.Add(citizen);
                    await _context.SaveChangesAsync();
                }

                appeal.CitizenId = citizen.Id;
                appeal.Status = "Зарегистрировано";
                appeal.RegistrationNumber = $"2026-{DateTime.Now.Ticks % 10000:D4}";
                appeal.Deadline = DateTime.Now.AddDays(15);

                _context.Appeals.Add(appeal);
                await _context.SaveChangesAsync();
                await _audit.LogAsync(User, "Create", "Appeal", appeal.Id);
                return RedirectToAction("My");
            }
            return View(appeal);
        }

        // ==================== НАЗНАЧЕНИЕ ИСПОЛНИТЕЛЯ ====================
        [Authorize(Roles = "Admin,Specialist")]
        public async Task<IActionResult> Assign(int id)
        {
            var appeal = await _context.Appeals
                .Include(a => a.Citizen)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appeal == null) return NotFound();

            ViewBag.Executors = await _userManager.GetUsersInRoleAsync("Executor");
            return View(appeal);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Specialist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, string executorId)
        {
            var appeal = await _context.Appeals.FindAsync(id);
            if (appeal == null) return NotFound();

            appeal.AssignedToUserId = executorId;
            appeal.Status = "На исполнении";
            await _context.SaveChangesAsync();
            await _audit.LogAsync(User, "Assign", "Appeal", id);
            return RedirectToAction("Details", new { id });
        }

        // ==================== СМЕНА СТАТУСА ====================
        [HttpPost]
        [Authorize(Roles = "Admin,Specialist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var appeal = await _context.Appeals.FindAsync(id);
            if (appeal == null) return NotFound();

            appeal.Status = status;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(User, "ChangeStatus", "Appeal", id);
            return RedirectToAction("Details", new { id });
        }

        // ==================== ЗАКРЫТИЕ ====================
        [HttpPost]
        [Authorize(Roles = "Admin,Specialist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var appeal = await _context.Appeals.FindAsync(id);
            if (appeal == null) return NotFound();

            appeal.Status = "Закрыто";
            await _context.SaveChangesAsync();
            await _audit.LogAsync(User, "Close", "Appeal", id);
            return RedirectToAction("Details", new { id });
        }

        // ==================== УДАЛЕНИЕ ====================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var appeal = await _context.Appeals.FindAsync(id);
            if (appeal == null) return NotFound();

            _context.Appeals.Remove(appeal);
            await _context.SaveChangesAsync();
            await _audit.LogAsync(User, "Delete", "Appeal", id);
            return RedirectToAction("Index");
        }

        // ==================== ДОБАВИТЬ ОТВЕТ ====================
        [HttpPost]
        [Authorize(Roles = "Admin,Executor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddResponse(int appealId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Details", new { id = appealId });

            var response = new Response
            {
                AppealId = appealId,
                Content = content,
                AuthorId = User.Identity?.Name ?? "unknown",
                CreatedAt = DateTime.Now
            };
            _context.Responses.Add(response);

            var appeal = await _context.Appeals.FindAsync(appealId);
            if (appeal != null) appeal.Status = "Ответ подготовлен";

            await _context.SaveChangesAsync();
            await _audit.LogAsync(User, "AddResponse", "Appeal", appealId);
            return RedirectToAction("Details", new { id = appealId });
        }
    }
}