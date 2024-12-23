using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace UserApi.Models
{
    public class Login
    {
        [BsonElement("email")]
        [Required]
        public string? Email { set; get; }

        [BsonElement("passwordHash")]
        [Required]
        public string? PasswordHash { set; get; }
    }
}