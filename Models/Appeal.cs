using System.ComponentModel.DataAnnotations;

namespace AppealsSystem.Models
{
    public class Appeal
    {
        public int Id { get; set; }

        [Display(Name = "Регистрационный номер")]
        public string? RegistrationNumber { get; set; }

        [Required(ErrorMessage = "Введите тему")]
        [Display(Name = "Тема обращения")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите текст")]
        [Display(Name = "Текст обращения")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Дата подачи")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Срок рассмотрения")]
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(15);

        [Display(Name = "Статус")]
        public string Status { get; set; } = "Зарегистрировано";

        [Display(Name = "Категория")]
        public string? Category { get; set; }

        [Display(Name = "Заявитель")]
        public int CitizenId { get; set; }
        public Citizen? Citizen { get; set; }

        [Display(Name = "Исполнитель")]
        public string? AssignedToUserId { get; set; }

        public List<Response> Responses { get; set; } = new();
        public List<Attachment> Attachments { get; set; } = new();
    }
}