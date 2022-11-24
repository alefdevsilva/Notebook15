using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Notebook.Authentication.Models.DTO.Incoming.Profile;
using Notebook15.DataService.IConfiguration;

namespace Notebook15.Api.Controllers.v1
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class ProfileController : BaseController
    {
        public ProfileController(IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager) : base(unitOfWork, userManager)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var loggerInUser = await _userManager.GetUserAsync(HttpContext.User);

            if (loggerInUser == null)
            {
                return BadRequest("User not found");
            }

            var identityId = new Guid(loggerInUser.Id);

            var profile = await _unitOfWork.Users.GetByIdentityId(identityId);
            if (profile == null)
            {
                return BadRequest("User not found");
            }

            return Ok(profile);    
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto profile)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid payload");
            }

            var loggerInUser = await _userManager.GetUserAsync(HttpContext.User);
            if (loggerInUser == null)
            {
                return BadRequest("User not found");
            }

            var identityId = new Guid(loggerInUser.Id);

            var userProfile = await _unitOfWork.Users.GetByIdentityId(identityId);
            if (userProfile == null)
            {
                return BadRequest("User not found");
            }

            userProfile.Address = profile.Address;
            userProfile.Sex = profile.Sex;
            userProfile.MobileNumber = profile.MobileNumber;
            userProfile.Country = profile.Country;

            var isUpdated = await _unitOfWork.Users.UpdateUserProfile(userProfile);
            if (isUpdated)
            {
                await _unitOfWork.CompleteAsync();
                return Ok(userProfile);
            }

            return BadRequest("Something went wrong, please try again later");
        }
    }
}