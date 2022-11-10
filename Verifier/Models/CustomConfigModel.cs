using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verifier.Models
{
    public class CustomConfigModel
    {
        public string VaultPassword { get; set; }
        public string EmailPostFix { get; set; }
        public string ChromeLocationPath { get; set; }
        public string Wallet { get; set; }
    }
}
