using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook.Authentication.Models.DTO.Incoming.Profile
{
    public class UpdateProfileDto
    {
        public string Country { get; set; }
        public string Address { get; set; }
        public string MobileNumber { get; set; }
        public string Sex { get; set; }
    }
}