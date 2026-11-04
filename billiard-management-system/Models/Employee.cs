using System;

namespace billiard_management_system.Models
{
    public class Employee
    {
        public long EmployeeId { get; set; }
        public long UserId { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime? HireDate { get; set; }
        public string Position { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
