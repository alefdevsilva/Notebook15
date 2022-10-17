using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notebook15.DataService.IConfiguration;
using Notebook15.Domain.Models;

namespace Notebook15.Api.Controllers.v1
{
    public class UserController : BaseController
    {
        public UserController(IUnitOfWork unitOfWork) : base(unitOfWork)
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