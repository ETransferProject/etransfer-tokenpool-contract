using AElf.Boilerplate.TestBase;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Standards.ACS0;
using AElf.Types;

namespace ETransfer.Contracts.TokenPool
{
    public class TokenPoolContractTestBase : DAppContractTestBase<TokenPoolContractTestModule>
    {
        
        // You can get address of any contract via GetAddress method, for example:
        // internal Address DAppContractAddress => GetAddress(DAppSmartContractAddressNameProvider.StringName);
        protected const int MinersCount = 1;
        
        internal Address TokenPoolContractAddress => GetAddress(TokenPoolContractAddressNameProvider.StringName);
        internal Account Admin => Accounts[0];
        internal Account User1 => Accounts[1];
        internal Account User2 => Accounts[2];
        internal Account User3 => Accounts[3];
        
        internal TokenContractContainer.TokenContractStub AdminTokenContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub User1TokenContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub User2TokenContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub User3TokenContractStub { get; set; }

        internal TokenPoolContractContainer.TokenPoolContractStub AdminTokenPoolContractStub { get; set; }
        internal TokenPoolContractContainer.TokenPoolContractStub User1TokenPoolContractStub { get; set; }
        internal TokenPoolContractContainer.TokenPoolContractStub User2TokenPoolContractStub { get; set; }
        internal TokenPoolContractContainer.TokenPoolContractStub User3TokenPoolContractStub { get; set; }
        
        protected readonly IBlockTimeProvider BlockTimeProvider;

        protected TokenPoolContractTestBase()
        {
            BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();

            AdminTokenContractStub = GetTokenContractStub(Admin.KeyPair);
            User1TokenContractStub = GetTokenContractStub(User1.KeyPair);
            User2TokenContractStub = GetTokenContractStub(User2.KeyPair);
            User3TokenContractStub = GetTokenContractStub(User3.KeyPair);

            AdminTokenPoolContractStub = GetContractStub(Admin.KeyPair);
            User1TokenPoolContractStub = GetContractStub(User1.KeyPair);
            User2TokenPoolContractStub = GetContractStub(User2.KeyPair);
            User3TokenPoolContractStub = GetContractStub(User3.KeyPair);
            
        }
        
        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }
        
        internal TokenPoolContractContainer.TokenPoolContractStub GetContractStub(ECKeyPair keyPair)
        {
            return GetTester<TokenPoolContractContainer.TokenPoolContractStub>(TokenPoolContractAddress, keyPair);
        }
        
    }
}