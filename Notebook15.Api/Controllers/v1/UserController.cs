using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Notebook15.DataService.IConfiguration;
using Notebook15.Domain.DbSet;

namespace Notebook15.Api.Controllers.v1
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : BaseController
    {
        public UserController(IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager) : base(unitOfWork, userManager)
        {
        } 

        [HttpGet]
        [Route("GetUser", Name = "GetUser")]
        public async Task<IActionResult> GetUser(Guid id){
            return Ok( await _unitOfWork.Users.GetById(id));
        }

        [HttpGet]
        [HttpHead]
        public async Task<IActionResult> GetUsers(){
            var users = await  _unitOfWork.Users.All();
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(User user){
            
           await  _unitOfWork.Users.Add(user);
           await  _unitOfWork.CompleteAsync();
    
            return CreatedAtRoute("GetUser", new {id = user.Id}, user);
        }
    }
}