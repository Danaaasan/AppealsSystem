namespace AppealsSystem.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public int AppealId { get; set; }
        public Appeal? Appeal { get; set; }
    }
}
