using System;

namespace billiard_management_system.Models
{
    public class User
    {
        public long UserId { get; set; }
        public int RoleId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation / Display property
        public string RoleName { get; set; }
    }
}

