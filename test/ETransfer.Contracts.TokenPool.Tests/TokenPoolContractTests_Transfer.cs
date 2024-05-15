using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace ETransfer.Contracts.TokenPool
{
    public partial class TokenPoolContractTests
    {
        [Fact]
        public async Task TransferTest()
        {
            await InitTest();
            
            var holderList =
                await AdminTokenPoolContractStub.GetPoolInfo.CallAsync(new GetPoolInfoInput { Symbol = USDT });
            holderList.TokenHolders.Count.ShouldBe(1);
            var tokenHolderAddress =
                ConvertToVirtualAddress(holderList.TokenHolders[0].VirtualHash, TokenPoolContractAddress);

            // init usdt amount
            await AdminTokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = User1.Address,
                Symbol = USDT,
                Amount = 100_000000,
            });

            
            // user approve and transfer
            await User1TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = TokenPoolContractAddress,
                Symbol = USDT,
                Amount = 100_000000
            });
            var transferRes = await User1TokenPoolContractStub.TransferToken.SendAsync(new TransferTokenInput
            {
                Symbol = USDT,
                Amount = 100_000000
            });
            
            // verify TokenPoolTransferred
            transferRes.TransactionResult.Logs.Count(log => log.Name == nameof(TokenPoolTransferred)).ShouldBe(1);
            var log = TokenPoolTransferred.Parser.ParseFrom(transferRes.TransactionResult.Logs
                .First(log => log.Name == nameof(TokenPoolTransferred)).NonIndexed);
            log.From.ShouldBe(User1.Address);
            log.To.ShouldBe(tokenHolderAddress);
            log.Symbol.ShouldBe(USDT);
            log.Amount.ShouldBe(100_000000);
            
            // verify Transferred
            transferRes.TransactionResult.Logs.Count(log => log.Name == nameof(Transferred)).ShouldBe(1);
            var transferred = Transferred.Parser.ParseFrom(transferRes.TransactionResult.Logs
                .First(log => log.Name == nameof(Transferred)).NonIndexed);
            transferred.Amount.ShouldBe(100_000000);
            
            
            // verify fund pool balance
            var balance = await AdminTokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = tokenHolderAddress,
                Symbol = USDT
            });
            balance.Balance.ShouldBe(100_000000);
            
            // admin withdraw
            var res = await AdminTokenPoolContractStub.Withdraw.SendAsync(new WithdrawInput
            {
                Symbol = USDT,
                Amount = 100_000000,
                VirtualHash = holderList.TokenHolders[0].VirtualHash
            });
            
            balance = await AdminTokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = tokenHolderAddress,
                Symbol = USDT
            });
            balance.Balance.ShouldBe(0);
            
        }
    }
}