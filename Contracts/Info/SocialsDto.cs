namespace CMS.Contracts.Info
{
    public class SocialsDto
    {
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Facebook { get; set; } = string.Empty;
    }

    // For PATCH – all nullable so “missing” means “don’t change”
    public class SocialsPatchDto
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Facebook { get; set; }
    }
}
