using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Shouldly;
using Xunit.Abstractions;

namespace ETransfer.Contracts.TokenPool
{
    public partial class TokenPoolContractTests : TokenPoolContractTestBase
    {
        private const string USDT = "USDT";
        private const string ETH = "ETH";
        private const string ELF = "ELF";
        
        
        private readonly ITestOutputHelper _outputHelper;

        public TokenPoolContractTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        
        public Address ConvertToVirtualAddress(
            Hash virtualHash,
            Address contractAddress)
        {
            return Address.FromPublicKey(contractAddress.Value.Concat(virtualHash.Value.ToByteArray().ComputeHash()).ToArray());
        }
        
        private async Task InitUSDT()
        {
            await AdminTokenContractStub.Create.SendAsync(new CreateInput()
            {
                Symbol = "SEED-0",
                TokenName = "SEED—collection",
                TotalSupply = 1,
                Decimals = 0,
                Issuer = Admin.Address,
                IsBurnable = false,
                IssueChainId = 0,
                ExternalInfo = new ExternalInfo()
            });
            
            await AdminTokenContractStub.Create.SendAsync(new CreateInput()
            {
                Symbol = "SEED-1",
                TokenName = "SEED USDT",
                TotalSupply = 1,
                Decimals = 0,
                Issuer = Admin.Address,
                IsBurnable = true,
                IssueChainId = 0,
                LockWhiteList = { TokenContractAddress },
                ExternalInfo = new ExternalInfo
                {
                    Value = { 
                        new Dictionary<string, string>
                        {
                            ["__seed_owned_symbol"] = USDT,
                            ["__seed_exp_time"] = "9992145642"
                        }
                    }
                }
            });
            
            await AdminTokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "SEED-1",
                To = Admin.Address,
                Amount = 1
            });

            await AdminTokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = USDT,
                TokenName = "Tether token",
                TotalSupply = 100_000_000_000_000_000,// 100 billion
                Decimals = 6,
                Issuer = Admin.Address,
                IsBurnable = true,
                LockWhiteList = { TokenContractAddress },
                IssueChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
                Owner = Admin.Address
            });

            await AdminTokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = USDT,
                To = Admin.Address,
                Amount = 100_000_000_000_000_000
            });
            
            var balance = await AdminTokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Admin.Address,
                Symbol = USDT
            });
            balance.Balance.ShouldBe(100_000_000_000_000_000);
        }
        
    }
}