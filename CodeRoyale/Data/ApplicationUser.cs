using CodeRoyale.Data.Games;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CodeRoyale.Data;
// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public const int MaxNameLength = GamePlayer.MaxNameLength;

    /// <summary>
    /// The name used when hosting games.
    /// </summary>
    [StringLength(MaxNameLength)]
    public string? Name { get; set; }

    internal string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return Name;
        }
        else if (!string.IsNullOrEmpty(UserName))
        {
            // trim just start of email
            int index = UserName.IndexOf('@');
            return index > 0 ? UserName[..index] : UserName;
        }

        return Id;
    }
}