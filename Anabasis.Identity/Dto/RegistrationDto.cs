using Anabasis.Identity.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Identity.Dto
{
    public class RegistrationDto: IRegistrationDto
    {

        [Required(ErrorMessage = "Username is required")]
        public string? Username { get; init; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; init; }

        [Required(ErrorMessage = "UserEmail is required")]
        public string? UserEmail { get; init; }

    }
}
