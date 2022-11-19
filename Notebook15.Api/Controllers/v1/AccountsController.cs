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
using Notebook15.Domain.DbSet;
using Notebook.Authentication.Models.DTO.Generic;

namespace Notebook15.Api.Controllers.v1
{
    public class AccountsController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtConfig _jwtConfig;
        
        public AccountsController(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            TokenValidationParameters tokenValidationParameters,
            IOptionsMonitor<JwtConfig> optionsMonitor) : base(unitOfWork)
        {
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
            _tokenValidationParameters = tokenValidationParameters;
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

            var token = await GenerateJwtToken(newUser);

            return Ok(new UserRegistrationResponseDto(){
                Success = true,
                Token = token.JwtToken,
                RefreshToken = token.RefreshToken
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
                var jwtToken = await GenerateJwtToken(userExist);

                return Ok(new UserLoginResposeDto(){
                    Success = true,
                    Token = jwtToken.JwtToken,
                    RefreshToken = jwtToken.RefreshToken
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

        [HttpPost]
        [Route("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto tokenRequestDto)
        {
            if (ModelState.IsValid)
            {
                var result = await VerifyToken(tokenRequestDto);

                if(result == null)
                {
                     return BadRequest(new UserRegistrationResponseDto{
                    Success = false,
                    Errors = new List<string>(){
                        "Token validation failed"
                        }
                    });
                }
                return Ok(result);
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

        private async Task<AuthResult> VerifyToken(TokenRequestDto tokenRequestDto)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(tokenRequestDto.Token, _tokenValidationParameters, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                    if (!result)
                        return null;   
                }

                var utcExpireDate = long.Parse(principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

                var expDate = UnixTimeStampToDateTime(utcExpireDate);

                if (expDate > DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Jwt token has not expired"
                        }
                    };
                }

                var refreshTokenExist = await _unitOfWork.RefreshTokens.GetByRefreshToken(tokenRequestDto.RefreshToken);

                if (refreshTokenExist == null)
                {
                   return new AuthResult()
                   {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Invalid refresh token"
                        }
                   };
                }

                if (refreshTokenExist.ExpiryDate < DateTime.UtcNow)
                {
                     return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Refresh token has expired, please login again"
                        }
                    };
                }

                if (refreshTokenExist.IsUsed)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Refresh token has been used, it cannot be reused"
                        }
                    };
                }

                if (refreshTokenExist.IsRevoked)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Refresh token has been revoked, it cannot be reused"
                        }
                    };
                }

                var jti = principal.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

                if(refreshTokenExist.JwtId != jti)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Refresh token reference does not match the jwt token"
                        }
                    };
                }

                refreshTokenExist.IsUsed = true;

                var updateResult = await _unitOfWork.RefreshTokens.MarkRefreshTokenAsUsed(refreshTokenExist);
                
                if(updateResult)
                {
                    await _unitOfWork.CompleteAsync();

                    var dbUser = await _userManager.FindByIdAsync(refreshTokenExist.UserId);
                    if (dbUser != null)
                    {
                        return new AuthResult()
                        {
                            Success = false,
                            Errors = new List<string>()
                            {
                                "Error processing request"
                            }
                        };
                    }

                    var tokens = await GenerateJwtToken(dbUser);

                    return new AuthResult
                    {
                        Token = tokens.JwtToken,
                        Success = true,
                        RefreshToken = tokens.RefreshToken
                    };
                }

                return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Error processing request"
                        }
                    };
            }
            catch (Exception ex)
            {   Console.WriteLine(ex.Message);
                // TODO: Add better error handling, and the logger
                return null;
            }
        }

        private DateTime UnixTimeStampToDateTime(long unixDate)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixDate).ToUniversalTime();
            return dateTime;
        }

        private async Task<TokenData> GenerateJwtToken(IdentityUser user)
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
                Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = jwtHandler.CreateToken(tokenDescriptor);

            var jwtToken = jwtHandler.WriteToken(token);


            var refreshToken = new RefreshToken
            {
                Token = $"{RandonStringGenerator(25)}_{Guid.NewGuid()}",
                UserId = user.Id,
                IsRevoked = false,
                IsUsed = false,
                Status = 1,
                JwtId = token.Id,
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };

            await _unitOfWork.RefreshTokens.Add(refreshToken);
            await _unitOfWork.CompleteAsync();

            var tokenData = new TokenData
            {
                JwtToken = jwtToken,
                RefreshToken = refreshToken.Token
            };

            return tokenData;
        }

        private string RandonStringGenerator(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWVXYZ123456789";

            return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}