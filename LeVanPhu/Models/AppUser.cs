using Microsoft.AspNetCore.Identity;
namespace LeVanPhu.Api.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public string? CreatedByUserId { get; set; }
}
