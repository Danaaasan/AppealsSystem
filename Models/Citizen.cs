using System.ComponentModel.DataAnnotations;

namespace AppealsSystem.Models
{
    public class Citizen
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите ФИО")]
        [Display(Name = "ФИО")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Адрес")]
        public string? Address { get; set; }

        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        // Зашифрованные ПДн (паспорт)
        public string? EncryptedPassport { get; set; }

        public List<Appeal> Appeals { get; set; } = new();
    }
}