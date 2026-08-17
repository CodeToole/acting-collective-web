using System.ComponentModel.DataAnnotations;

namespace theactingcollective.Models;

public class WaitlistSignup
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(80, ErrorMessage = "Name cannot exceed 80 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
