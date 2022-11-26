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
    public abstract class BaseUserManagementController<TRegistrationDto,TUserLoginResponse,TUser> : ControllerBase 
        where TUser : class, IHaveEmail
        where TRegistrationDto: IRegistrationDto
    {
        private readonly IPasswordResetMailService _passwordResetMailService;

        protected UserManager<TUser> UserManager { get; }

        public BaseUserManagementController(IPasswordResetMailService passwordResetMailService,
            UserManager<TUser> userManager)
        {
            _passwordResetMailService = passwordResetMailService;
            UserManager = userManager;
        }

        protected abstract Task<TUser> CreateUser(TRegistrationDto registrationDto);
        protected abstract Task<TUserLoginResponse> GetLoginResponse(TUser user);

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
        {
            var user = await UserManager.FindByNameAsync(userLoginDto.Username);

            if (null == user)
            {
                return NotFound($"User with username {userLoginDto.Username} doesn't exist").WithErrorFormatting();
            }

            var checkPasswordResult = await UserManager.CheckPasswordAsync(user, userLoginDto.Password);

            if (!checkPasswordResult)
            {
                return BadRequest($"Password for user {userLoginDto.Username} is not valid").WithErrorFormatting();
            }

            var userLoginResponse = GetLoginResponse(user);

            return Ok(userLoginResponse);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] TRegistrationDto registrationDto)
        {
           
            var user = await CreateUser(registrationDto);

            var userResult = await UserManager.CreateAsync(user, registrationDto.Password);

            if (!userResult.Succeeded)
            {
                return BadRequest(userResult).WithErrorFormatting();
            }

            var createdUser = await UserManager.FindByEmailAsync(registrationDto.UserEmail);

            if (null == createdUser)
            {
                return StatusCode(500, "User failed to be created").WithErrorFormatting();
            }

            var emailConfirmationToken = await UserManager.GenerateEmailConfirmationTokenAsync(createdUser);

            await _passwordResetMailService.SendEmailPasswordReset(registrationDto.UserEmail, emailConfirmationToken);

            var registrationResponseDto = new RegistrationResponseDto(registrationDto.Username, registrationDto.UserEmail);

            return StatusCode(201, registrationResponseDto);
        }

        [AllowAnonymous]
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmMailDto confirmMailDto)
        {
            var user = await UserManager.FindByEmailAsync(confirmMailDto.Email);

            if (null == user)
            {
                return NotFound($"User with mail {confirmMailDto.Email} was not found").WithErrorFormatting();
            }

            var confirmEmailResult = await UserManager.ConfirmEmailAsync(user, confirmMailDto.Token);

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

            var user = await UserManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (null == user)
            {
                return NotFound($"User with mail {forgotPasswordDto.Email} was not found").WithErrorFormatting();
            }

            var token = await UserManager.GeneratePasswordResetTokenAsync(user);

            await _passwordResetMailService.SendEmailPasswordReset(user.Email, token);

            return Accepted();

        }

        [AllowAnonymous]
        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPassword)
        {

            var user = await UserManager.FindByEmailAsync(resetPassword.Email);

            if (null == user)
            {
               return NotFound(new Exception ( $"User with mail {resetPassword.Email} was not found" )).WithErrorFormatting();
            }

            var resetPassResult = await UserManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);

            if (!resetPassResult.Succeeded)
            {
                return BadRequest(resetPassResult).WithErrorFormatting();
            }

            return Ok();
        }

    }
}
