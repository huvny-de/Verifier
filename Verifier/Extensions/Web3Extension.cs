using Nethereum.Signer;
using System.Collections.Generic;

namespace Verifier.Extensions
{
    public static class Web3Extension
    {
        public static string GenerateWallet()
        {
            EthECKey key = EthECKey.GenerateKey();
            string address = key.GetPublicAddress();
            return address;
        }

        private static List<string> GenerateWalletAndprivateKeyAsync(int total)
        {
            List<string> value = new List<string>();
            for (int i = 0; i < total; i++)
            {

                EthECKey key = EthECKey.GenerateKey();
                string privateKey = key.GetPrivateKey();
                string address = key.GetPublicAddress();
                value.Add(address + ":" + privateKey);
            }
            return value;
        }

        public static (string, string) GenerateWalletAndpublicKeyAsync()
        {
            EthECKey key = EthECKey.GenerateKey();
            string publicKey = key.GetPrivateKey();
            string address = key.GetPublicAddress();
            return (publicKey, address);
        }
    }
}
