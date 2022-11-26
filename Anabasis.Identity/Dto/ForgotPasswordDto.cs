using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity.Dto
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required")]
        public string? Email { get; init; }
    }
}
