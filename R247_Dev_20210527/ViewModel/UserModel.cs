using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.ViewModel
{
    public class UserModel
    {
       public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class CurrentUserRole
    {
        public int Id { get; set; }
        public string Role { get; set; }
        public bool EditJob { get; set; }
        public bool EditTool { get; set; }
        public bool Privilege { get; set; }
        public bool UserManagement { get; set; }
        
    }
}
