using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BookStore_API.Contracts;
using BookStore_API.Mapp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BookStore_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private ILoggerService _loggerService;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _config;

        public UsersController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILoggerService loggerService, IConfiguration config)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _loggerService = loggerService;
            _config = config;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserDTO userDTO)
        {
            try
            {
                if (userDTO == null || string.IsNullOrEmpty(userDTO.Username) || string.IsNullOrEmpty(userDTO.Password))
                {
                    _loggerService.LogWarn("User DTO is null");
                    return BadRequest();
                }

                var result = await _signInManager.PasswordSignInAsync(userDTO.Username, userDTO.Password, false, false);
                if (!result.Succeeded)
                {
                    _loggerService.LogWarn("Sign in failed");
                    return NotFound();
                }

                var user = await _userManager.FindByNameAsync(userDTO.Username);
                var tokenString = await GenerateJSONWebToken(user);
                return Ok(new { token = tokenString });
            }
            catch (Exception ex)
            {
                _loggerService.LogError(ex.Message + ex.StackTrace);
                return StatusCode(500, "Something went wrong. Please contact the Administrator");
            }
        }

        private async Task<string> GenerateJSONWebToken(IdentityUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };
            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(ClaimsIdentity.DefaultRoleClaimType, r)));

            var token = new JwtSecurityToken(_config["Jwt:Issuer"]
                , _config["Jwt:Issuer"],
                claims,
                null,
                expires: DateTime.Now.AddHours(5),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
