using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verifier.InputModels
{
    public class CoinTeleGraphIM
    {
        public string Email { get; set; }
        public string TargetUrl { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Wallet { get; set; }
        public string ApiKey { get; set; }
    }
}
