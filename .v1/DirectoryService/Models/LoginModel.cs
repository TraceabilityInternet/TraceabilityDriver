using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DirectoryService.Models
{
    [DataContract]
    public class LoginModel
    {
        [Required]
        [DataMember]
        public string Email { get; set; }

        [Required]
        [DataMember]
        public string Password { get; set; }
    }
}
