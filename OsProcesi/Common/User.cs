using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class User
    {
        public string Name
        {
            get => _name;
            set => _name = value;
        }



        public string Password
        {
            get => _password;
            private set => _password = value;
        }



        public User(string name, string password)
        {
            Name = name;
            Password = password;
        }



        private string _name = string.Empty;
        private string _password = string.Empty;
    }
}
