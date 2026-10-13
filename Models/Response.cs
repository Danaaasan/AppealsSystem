using System.ComponentModel.DataAnnotations;

namespace AppealsSystem.Models
{
    public class Response
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Текст ответа")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Дата ответа")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Автор")]
        public string AuthorId { get; set; } = string.Empty;

        public int AppealId { get; set; }
        public Appeal? Appeal { get; set; }
    }
}