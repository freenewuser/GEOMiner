using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GEOMiner.Models
{
    public class SessionModel
    {
        public string fileName { get; set; }

        
        public IFormFile file { get; set; }

        public string fileString { get; set; }

        public string validationMessage { get; set; }
        public bool valError { get; set; }
    }
}
