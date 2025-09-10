using System.ComponentModel.DataAnnotations;

namespace CMS.Models.Metrics
{
    public class PageViewDaily
    {
        [Key] public long Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public DateTime DayUtc { get; set; } // 00:00 UTC for the day
        public int Count { get; set; }
    }
}
