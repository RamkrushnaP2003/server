using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserApi.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; }  

        [BsonElement("firstName")]
        public string? FirstName { get; set; }

        [BsonElement("lastName")]

        public string? LastName { get; set; }

        [BsonElement("email")]
        [Required]

        public string? Email { get; set; }

        [BsonElement("passwordHash")]
        [Required]
        public string? PasswordHash { get; set; }

        [BsonElement("phone")]
        [Required]
        public string? Phone { get; set; }

        [BsonElement("profileImagePath")]
        public string ProfileImagePath { get; set; } = "";

        [BsonElement("publishedBook")]
        public List<Book> PublishedBook { get; set; } = new List<Book>();

        [BsonElement("wishlist")]
        public List<Book> Wishlist { get; set; } = new List<Book>();

        [BsonElement("cart")]
        public List<Book> Cart { get; set; } = new List<Book>();
    }
}
