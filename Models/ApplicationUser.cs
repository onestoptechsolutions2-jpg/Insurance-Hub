using Microsoft.AspNetCore.Identity;

namespace Insurance_Hub.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}
