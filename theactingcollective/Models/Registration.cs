using System.ComponentModel.DataAnnotations;

namespace theactingcollective.Models;

/// <summary>
/// A single actor's registration for an Acting Collective class.
/// Data-annotation attributes drive Blazor's EditForm validation automatically,
/// so the same rules protect us on the client AND when we swap in Azure later.
/// </summary>
public class Registration
{
    // Partition/RowKey-friendly id for the eventual Azure Table Storage move.
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(60)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a valid email.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your mobile number.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose an experience level.")]
    public string ExperienceLevel { get; set; } = string.Empty;

    public string? Referral { get; set; }

    // Required-checkbox trick: the value must equal "true" to pass validation.
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm you are 18 or older.")]
    public bool AgeConfirmed { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please agree to receive class communication.")]
    public bool CommsConsent { get; set; }

    public bool WantsNewsletter { get; set; }

    // Class-day + admin fields
    public string RegType { get; set; } = "standard";      // standard | vip | walkin
    public bool CheckedIn { get; set; }
    public DateTimeOffset? CheckedInAt { get; set; }
    public bool Paid { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Convenience helpers used by the roster UI
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Initials =>
        string.Concat((FirstName.FirstOrDefault(), LastName.FirstOrDefault())).ToUpperInvariant();

    public static readonly string[] ExperienceLevels =
    {
        "Brand New",
        "Some Classes or Workshops",
        "Stage Experience",
        "On-Camera Experience",
        "Working Actor"
    };
}

