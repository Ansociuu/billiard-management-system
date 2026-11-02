using System;

namespace billiard_management_system.Models
{
    public class Customer
    {
        public long CustomerId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}

