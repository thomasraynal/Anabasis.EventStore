using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Anabasis.Api.Shared;
using Anabasis.Identity.Dto;
using Anabasis.Identity.Shared;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Web;

namespace Anabasis.Identity
{
    public abstract class BaseUserManagementController<TRegistrationDto, TUserLoginDto, TUserLoginResponse,TUser> : ControllerBase 
        where TUser : class, IHaveEmail
        where TRegistrationDto: IRegistrationDto
         where TUserLoginDto : IUserLoginDto
    {
        private readonly IUserMailService _userMailService;

        protected UserManager<TUser> UserManager { get; }

        public BaseUserManagementController(IUserMailService passwordResetMailService,
            UserManager<TUser> userManager)
        {
            _userMailService = passwordResetMailService;
            UserManager = userManager;
        }

        protected abstract Task<TUser> CreateUser(TRegistrationDto registrationDto);
        protected abstract Task<TUserLoginResponse> GetLoginResponse(TUserLoginDto userLoginDto, TUser user);

        protected virtual Task OnUserRegistered(TUser user)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnUserLoginSuccess(TUserLoginResponse userLoginResponse)
        {
            return Task.CompletedTask;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] TUserLoginDto userLoginDto)
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

            var userLoginResponse = await GetLoginResponse(userLoginDto, user);

            await OnUserLoginSuccess(userLoginResponse);

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
                return BadRequest(userResult.FlattenErrors()).WithErrorFormatting();
            }

            var createdUser = await UserManager.FindByEmailAsync(registrationDto.UserEmail);

            if (null == createdUser)
            {
                return StatusCode(500, "User failed to be created").WithErrorFormatting();
            }

            var emailConfirmationToken = await UserManager.GenerateEmailConfirmationTokenAsync(createdUser);

            var httpEncodedToken = HttpUtility.UrlEncode(emailConfirmationToken);

            await _userMailService.SendEmailConfirmationToken(registrationDto.UserEmail, httpEncodedToken);

            var registrationResponseDto = new RegistrationResponseDto(registrationDto.Username, registrationDto.UserEmail);

            await OnUserRegistered(createdUser);

            return StatusCode(201, registrationResponseDto);
        }

        [AllowAnonymous]
        [HttpGet("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] ConfirmMailDto confirmMailDto)
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
                return BadRequest(confirmEmailResult.FlattenErrors()).WithErrorFormatting();
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

            var generatePasswordResetToken = await UserManager.GeneratePasswordResetTokenAsync(user);

            var httpEncodedToken = HttpUtility.UrlEncode(generatePasswordResetToken);

            await _userMailService.SendEmailPasswordReset(user.UserEmail, generatePasswordResetToken);

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
                return BadRequest(resetPassResult.FlattenErrors()).WithErrorFormatting();
            }

            return Ok();
        }

    }
}
