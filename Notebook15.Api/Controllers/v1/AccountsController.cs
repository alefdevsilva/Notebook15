using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Notebook.Authentication.Configuration;
using Notebook.Authentication.Models.DTO.Incoming;
using Notebook15.DataService.IConfiguration;
using Notebook.Authentication.Models.DTO.Outgoing;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Notebook15.Domain.Models;

namespace Notebook15.Api.Controllers.v1
{
    public class AccountsController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        public AccountsController(
            IUnitOfWork unitOfWork, 
            UserManager<IdentityUser> userManager,
            IOptionsMonitor<JwtConfig> optionsMonitor) : base(unitOfWork)
        {
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto registrationDto)
        {
            if (ModelState.IsValid)
            {
                var userExist = await _userManager.FindByEmailAsync(registrationDto.Email);
                if (userExist != null)
                {
                    return BadRequest(new UserRegistrationResponseDto()
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Email already in use"
                        }
                    });
                }
            var newUser = new IdentityUser()
            {
                Email = registrationDto.Email,
                UserName = registrationDto.Email,
                EmailConfirmed = true // Todo build email funcionality to send to user to comfirm email,
            };     

            var isCreated = await _userManager.CreateAsync(newUser, registrationDto.Password);
            if (!isCreated.Succeeded)
            {
                return BadRequest(new UserRegistrationResponseDto()
                {
                    Success = isCreated.Succeeded,
                    Errors = isCreated.Errors.Select(x => x.Description).ToList()
                });
            }

            var _user = new User();
            _user.IdentityId = new Guid(newUser.Id);
            _user.FirstName = registrationDto.FirstName;
            _user.LastName = registrationDto.LastName;
            _user.Email = registrationDto.Email;
            _user.DateOfBirth = DateTime.UtcNow;
            _user.Phone = "";
            _user.Country = "";
            _user.Status = 1;

            await _unitOfWork.Users.Add(_user);
            await _unitOfWork.CompleteAsync();

            var token = GenerateJwtToken(newUser);

            return Ok(new UserRegistrationResponseDto(){
                Success = true,
                Token = token
            });

            }
            else
            {
                return BadRequest(new UserRegistrationResponseDto{
                    Success = false,
                    Errors = new List<string>(){
                        "Invalid payload"
                    }
                });
            }
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto loginDto)
        {
            if (ModelState.IsValid)
            {
                var userExist = await _userManager.FindByEmailAsync(loginDto.Email);
                if (userExist == null)
                {
                    return BadRequest(new UserLoginResposeDto(){
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Invalid authentication request"
                        }
                    });
                }

            var isCorrect = await _userManager.CheckPasswordAsync(userExist, loginDto.Password);
            
            if (isCorrect)
            {
                var jwtToken = GenerateJwtToken(userExist);

                return Ok(new UserLoginResposeDto(){
                    Success = true,
                    Token = jwtToken
                });
            }
            else
            {
                return BadRequest(new UserLoginResposeDto(){
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Invalid authentication request"
                        }
                    });
            }

            }
            else
            {
                return BadRequest(new UserRegistrationResponseDto{
                    Success = false,
                    Errors = new List<string>(){
                        "Invalid payload"
                    }
                });
            }
        }

        private string GenerateJwtToken(IdentityUser user)
        {
            var jwtHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new []
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(3),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = jwtHandler.CreateToken(tokenDescriptor);

            var jwtToken = jwtHandler.WriteToken(token);

            return jwtToken;
        }
    }
}