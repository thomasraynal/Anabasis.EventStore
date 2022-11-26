using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Anabasis.Api.Shared;
using Anabasis.Identity.Dto;
using Anabasis.Identity.Shared;

namespace Anabasis.Identity
{
    public abstract class BaseUserManagementController<TRegistrationDto,TUser> : ControllerBase 
        where TUser : class, IHaveEmail
        where TRegistrationDto: IRegistrationDto
    {
        private readonly IPasswordResetMailService _passwordResetMailService;
        private readonly UserManager<TUser> _userManager;
        private readonly IJwtTokenService<TUser> _tokenService;

        public BaseUserManagementController(IJwtTokenService<TUser> tokenService,
            IPasswordResetMailService passwordResetMailService,
            UserManager<TUser> userManager)
        {
            _passwordResetMailService = passwordResetMailService;
            _userManager = userManager;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
        {
            var user = await _userManager.FindByNameAsync(userLoginDto.UserName);

            if (null == user)
            {
                return NotFound($"User with username {userLoginDto.UserName} doesn't exist").WithErrorFormatting();
            }

            var checkPasswordResult = await _userManager.CheckPasswordAsync(user, userLoginDto.Password);

            if (!checkPasswordResult)
            {
                return BadRequest($"Password for user {userLoginDto.UserName} is not valid").WithErrorFormatting();
            }

            var (token, expirationUtcDate) = _tokenService.CreateToken(user);

            var userLoginResponse = new UserLoginResponse()
            {
                BearerToken = token,
                ExpirationUtcDate = expirationUtcDate
            };

            return Ok(userLoginResponse);
        }


        protected abstract Task<TUser> CreateUser(TRegistrationDto registrationDto);

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] TRegistrationDto registrationDto)
        {
           
            var user = await CreateUser(registrationDto);

            var userResult = await _userManager.CreateAsync(user, registrationDto.Password);

            if (!userResult.Succeeded)
            {
                return BadRequest(userResult).WithErrorFormatting();
            }

            var createdUser = await _userManager.FindByEmailAsync(registrationDto.UserMail);

            if (null == createdUser)
            {
                return StatusCode(500, "User failed to be created").WithErrorFormatting();
            }

            var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(createdUser);

            var registrationResponseDto = new RegistrationResponseDto(registrationDto.UserName, registrationDto.UserMail, emailConfirmationToken);

            return StatusCode(201, registrationResponseDto);
        }

        [AllowAnonymous]
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmMailDto confirmMailDto)
        {
            var user = await _userManager.FindByEmailAsync(confirmMailDto.Email);

            if (null == user)
            {
                return NotFound($"User with mail {confirmMailDto.Email} was not found").WithErrorFormatting();
            }

            var confirmEmailResult = await _userManager.ConfirmEmailAsync(user, confirmMailDto.Token);

            if (confirmEmailResult.Succeeded)
            {
                return Ok();
            }
            else
            {
                return BadRequest(confirmEmailResult).WithErrorFormatting();
            }
        }

        [AllowAnonymous]
        [HttpPost("forgot")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {

            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (null == user)
            {
                return NotFound($"User with mail {forgotPasswordDto.Email} was not found").WithErrorFormatting();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            await _passwordResetMailService.SendEmailPasswordReset(user.Email, token);

            return Accepted();

        }

        [AllowAnonymous]
        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPassword)
        {

            var user = await _userManager.FindByEmailAsync(resetPassword.Email);

            if (null == user)
            {
               return NotFound(new Exception ( $"User with mail {resetPassword.Email} was not found" )).WithErrorFormatting();
            }

            var resetPassResult = await _userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);

            if (!resetPassResult.Succeeded)
            {
                return BadRequest(resetPassResult).WithErrorFormatting();
            }

            return Ok();
        }

    }
}
