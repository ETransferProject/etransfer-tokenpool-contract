using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace ETransfer.Contracts.TokenPool
{
    public partial class TokenPoolContractTests
    {
        [Fact]
        public async Task ReleaseTest()
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

            var noPermission = await Assert.ThrowsAnyAsync<Exception>(() =>
                AdminTokenPoolContractStub.ReleaseToken.SendAsync(new ReleaseTokenInput
                {
                    Symbol = USDT,
                    Amount = 100_000000,
                    To = User1.Address
                }));
            noPermission.Message.ShouldContain("No permission");
            
            // add permission
            await AdminTokenPoolContractStub.AddReleaseController.SendAsync(new ControllerInput
            {
                Address = User1.Address
            });
            var releaseControllers = await AdminTokenPoolContractStub.GetReleaseControllers.CallAsync(new Empty());
            releaseControllers.Addresses.Count.ShouldBe(1);

            // release
            var res = await User1TokenPoolContractStub.ReleaseToken.SendAsync(new ReleaseTokenInput()
            {
                Symbol = USDT,
                Amount = 100_000000,
                To = User1.Address
            });

            // verify TokenPoolReleased
            res.TransactionResult.Logs.Count(log => log.Name == nameof(TokenPoolReleased)).ShouldBe(1);
            var log = TokenPoolTransferred.Parser.ParseFrom(res.TransactionResult.Logs
                .First(log => log.Name == nameof(TokenPoolReleased)).NonIndexed);
            log.From.ShouldBe(tokenHolderAddress);
            log.To.ShouldBe(User1.Address);
            log.Symbol.ShouldBe(USDT);
            log.Amount.ShouldBe(100_000000);
            
            // verify Transferred
            res.TransactionResult.Logs.Count(log => log.Name == nameof(Transferred)).ShouldBe(1);
            var transferred = Transferred.Parser.ParseFrom(res.TransactionResult.Logs
                .First(log => log.Name == nameof(Transferred)).NonIndexed);
            transferred.Amount.ShouldBe(100_000000);
            
            // verify balance
            balance = await AdminTokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = tokenHolderAddress,
                Symbol = USDT
            });
            balance.Balance.ShouldBe(0);
            
            balance = await AdminTokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = User1.Address,
                Symbol = USDT
            });
            balance.Balance.ShouldBe(100_000000);
            
        }
    }
}