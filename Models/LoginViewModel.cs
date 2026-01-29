using System.ComponentModel.DataAnnotations;

namespace HMSCore.Models
{
    public class LoginViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string Username { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
 
}
}
