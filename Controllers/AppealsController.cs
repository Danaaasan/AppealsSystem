using AppealsSystem.Data;
using AppealsSystem.Models;
using AppealsSystem.Services;
using Microsoft.AspNetCore.Authorization;
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

        public AppealsController(ApplicationDbContext context, IEncryptionService encryption, IAuditService audit)
        {
            _context = context;
            _encryption = encryption;
            _audit = audit;
        }

        [Authorize(Roles = "Admin,Specialist,Executor")]
        public async Task<IActionResult> Index()
        {
            var appeals = await _context.Appeals
                .Include(a => a.Citizen)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(appeals);
        }

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

        public async Task<IActionResult> Details(int id)
        {
            var appeal = await _context.Appeals
                .Include(a => a.Citizen)
                .Include(a => a.Responses)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appeal == null) return NotFound();

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

            return View(appeal);
        }

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

        [HttpPost]
        [Authorize(Roles = "Admin,Executor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddResponse(int appealId, string content)
        {
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