using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnCallTrackingSystem.Models
{
    public class Employee
    {
        public string Email;
        public int Id;
        public string FirstName;
        public string LastName;
        public string PhoneNumber;
        public string EmployeeId;
        public string FullName { get { return FirstName + " " + LastName; } }
    }
}
