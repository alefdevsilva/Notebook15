using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Notebook.Authentication.Models.DTO.Incoming
{
    public class TokenRequestDto
    {
        [Required]
        public string Token { get;  set; }
        
        [Required]
        public string RefreshToken { get; set; }
    }
}