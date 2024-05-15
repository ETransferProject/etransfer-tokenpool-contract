using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace ETransfer.Contracts.TokenPool
{
    public partial class TokenPoolContractTests
    {
        [Fact]
        public async Task<TransactionResult> InitTest()
        {
            await InitUSDT();

            var res = await AdminTokenPoolContractStub.Initialize.SendAsync(new InitializeInput
            {
                TokenSymbolList = new TokenSymbolList
                {
                    Value = { USDT }
                }
            });

            var tokenSymbols = await AdminTokenPoolContractStub.GetSymbolTokens.CallAsync(new Empty());
            tokenSymbols.Value.Count().ShouldBe(1);
            tokenSymbols.Value[0].ShouldBe(USDT);

            var poolAccount =
                await AdminTokenPoolContractStub.GetPoolInfo.CallAsync(new GetPoolInfoInput { Symbol = USDT });
            poolAccount.TokenHolders.Count.ShouldBe(1);

            res.TransactionResult.Logs.Count(log => log.Name == nameof(TokenPoolAdded)).ShouldBe(1);
            var symbolAdded = TokenPoolAdded.Parser.ParseFrom(res.TransactionResult.Logs
                .First(log => log.Name == nameof(TokenPoolAdded)).NonIndexed);
            symbolAdded.Symbol.ShouldBe(USDT);

            res.TransactionResult.Logs.Count(log => log.Name == nameof(TokenHolderAdded)).ShouldBe(1);
            var holderAdded = TokenHolderAdded.Parser.ParseFrom(res.TransactionResult.Logs
                .First(log => log.Name == nameof(TokenHolderAdded)).NonIndexed);
            holderAdded.Symbol.ShouldBe(USDT);
            holderAdded.TokenHolders.Value.Count.ShouldBe(1);

            return res.TransactionResult;
        }

        [Fact]
        public async Task Init_fail()
        {
            await InitUSDT();

            var invalidAddress = await Assert.ThrowsAnyAsync<Exception>(() =>
                AdminTokenPoolContractStub.Initialize.SendAsync(new InitializeInput
                {
                    TokenSymbolList = new TokenSymbolList
                    {
                        Value = { USDT }
                    },
                    Admin = new Address(),
                }));
            invalidAddress.Message.ShouldContain("Invalid admin address");

            var emptyTokenSymbol = await Assert.ThrowsAnyAsync<Exception>(() =>
                AdminTokenPoolContractStub.Initialize.SendAsync(new InitializeInput
                {
                    Admin = Admin.Address
                }));
            emptyTokenSymbol.Message.ShouldContain("Token symbol empty");
        }

        [Fact]
        public async Task SetAdmin()
        {
            await InitTest();

            var adminBefore = await AdminTokenPoolContractStub.GetAdmin.CallAsync(new Empty());
            adminBefore.ShouldBe(Admin.Address);

            await AdminTokenPoolContractStub.SetAdmin.SendAsync(User1.Address);

            var adminAfter = await AdminTokenPoolContractStub.GetAdmin.CallAsync(new Empty());
            adminAfter.ShouldBe(User1.Address);

            // failed 
            var noPermission = await Assert.ThrowsAnyAsync<Exception>(() =>
                AdminTokenPoolContractStub.SetAdmin.SendAsync(Admin.Address));
            noPermission.Message.ShouldContain("No permission");

            var invalidInput =
                await Assert.ThrowsAnyAsync<Exception>(() =>
                    User1TokenPoolContractStub.SetAdmin.SendAsync(new Address()));
            invalidInput.Message.ShouldContain("Invalid address");
        }

        [Fact]
        public async Task AddTokenPool()
        {
            await InitTest();

            // init one 
            var symbolList = await AdminTokenPoolContractStub.GetSymbolTokens.CallAsync(new Empty());
            symbolList.Value.Count.ShouldBe(1);

            var res = await AdminTokenPoolContractStub.AddTokenPool.SendAsync(new AddTokenPoolInput { Symbol = ELF });

            res.TransactionResult.Logs.Count(log => log.Name == nameof(TokenPoolAdded)).ShouldBe(1);
            res.TransactionResult.Logs.Count(log => log.Name == nameof(TokenHolderAdded)).ShouldBe(1);
            var tokenPoolAdded = TokenPoolAdded.Parser.ParseFrom(res.TransactionResult.Logs
                .First(log => log.Name == nameof(TokenPoolAdded)).NonIndexed);
            tokenPoolAdded.Symbol.ShouldBe(ELF);
            
            var tokenHolderAdded = TokenHolderAdded.Parser.ParseFrom(res.TransactionResult.Logs
                .First(log => log.Name == nameof(TokenHolderAdded)).NonIndexed);
            tokenHolderAdded.TokenHolders.Value.Count.ShouldBe(1);

            symbolList = await AdminTokenPoolContractStub.GetSymbolTokens.CallAsync(new Empty());
            symbolList.Value.Count.ShouldBe(2);


            var tokenPool = await AdminTokenPoolContractStub.GetPoolInfo.CallAsync(new GetPoolInfoInput
            {
                Symbol = ELF
            });

            tokenPool.Symbol.ShouldBe(ELF);
            tokenPool.TokenHolders.Count.ShouldBe(1);
        }

        [Fact]
        public async Task AddPoolTokenHolder()
        {
            await InitTest();

            // init one 
            var poolInfo =
                await AdminTokenPoolContractStub.GetPoolInfo.CallAsync(new GetPoolInfoInput { Symbol = USDT });
            poolInfo.TokenHolders.Count.ShouldBe(1);

            // add another
            var result = await AdminTokenPoolContractStub.AddTokenHolders.SendAsync(new AddTokenHolderInput
                { Symbol = USDT, HolderCount = 1 });
            result.TransactionResult.Logs.Count(log => log.Name == nameof(TokenHolderAdded)).ShouldBe(1);
            var tokenHolderAdded = TokenHolderAdded.Parser.ParseFrom(result.TransactionResult.Logs
                .First(log => log.Name == nameof(TokenHolderAdded)).NonIndexed);
            tokenHolderAdded.Symbol.ShouldBe(USDT);
            tokenHolderAdded.TokenHolders.Value.Count.ShouldBe(1);

            // now, query result is two
            poolInfo =
                await AdminTokenPoolContractStub.GetPoolInfo.CallAsync(new GetPoolInfoInput { Symbol = USDT });
            poolInfo.TokenHolders.Count.ShouldBe(2);

            _outputHelper.WriteLine(TokenPoolContractAddress.ToBase58());
            foreach (var tokenHolder in poolInfo.TokenHolders)
            {
                _outputHelper.WriteLine(tokenHolder.VirtualHash.ToHex() + ", " +
                                        ConvertToVirtualAddress(tokenHolder.VirtualHash, TokenPoolContractAddress)
                                            .ToBase58());
            }

            // add another 99 holders, will get max count exceeded error
            var maxExceeded = await Assert.ThrowsAnyAsync<Exception>(() =>
                AdminTokenPoolContractStub.AddTokenHolders.SendAsync(new AddTokenHolderInput
                    { Symbol = USDT, HolderCount = 99 }));
            maxExceeded.Message.ShouldContain("Pool holder max count exceeded");
        }


        [Fact]
        public async Task Withdraw()
        {
            await InitTest();

            var poolInfo =
                await AdminTokenPoolContractStub.GetPoolInfo.CallAsync(new GetPoolInfoInput { Symbol = USDT });
            poolInfo.TokenHolders.Count.ShouldBe(1);
            var tokenHolderAddress = poolInfo.TokenHolders[0].Address;

            await AdminTokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = tokenHolderAddress,
                Symbol = USDT,
                Amount = 100_000000,
            });

            var balance = await AdminTokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = tokenHolderAddress,
                Symbol = USDT
            });
            balance.Balance.ShouldBe(100_000000);

            // withdraw
            var res = await AdminTokenPoolContractStub.Withdraw.SendAsync(new WithdrawInput
            {
                Symbol = USDT,
                Amount = 100_000000,
                VirtualHash = poolInfo.TokenHolders[0].VirtualHash
            });

            // verify Transferred
            res.TransactionResult.Logs.Count(log => log.Name == nameof(Transferred)).ShouldBe(1);
            var transferred = Transferred.Parser.ParseFrom(res.TransactionResult.Logs
                .First(log => log.Name == nameof(Transferred)).NonIndexed);
            transferred.Amount.ShouldBe(100_000000);

            balance = await AdminTokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = tokenHolderAddress,
                Symbol = USDT
            });
            balance.Balance.ShouldBe(0);
        }
    }
}