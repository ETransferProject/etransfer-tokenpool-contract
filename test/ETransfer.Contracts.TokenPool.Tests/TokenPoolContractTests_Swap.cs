using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using ETransfer.Contracts.TestSwapContracts;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.Cms;
using Shouldly;
using Xunit;

namespace ETransfer.Contracts.TokenPool;

public partial class TokenPoolContractTests
{
    [Fact]
    public async Task SwapToken_Test()
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

        await AdminTokenPoolContractStub.SetSwapContractAddress.SendAsync(new SetSwapContractAddressInput
        {
            Value =
            {
                new SwapContract
                {
                    FeeRate = 300,
                    SwapContractAddress = TestSwapContractAddress
                }
            }
        });
        var swapContract = await AdminTokenPoolContractStub.GetSwapContracts.CallAsync(new Int64Value
        {
            Value = 300
        });
        swapContract.ShouldBe(TestSwapContractAddress);

        var result = await AdminTokenPoolContractStub.SwapToken.SendWithExceptionAsync(new SwapTokenInput
        {
            AmountIn = 10000000,
            AmountOutMin = 5000000,
            Channel = "test",
            Deadline = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 3))),
            To = User1.Address,
            FeeRate = 300,
            Path = { "USDT", "ELF" }
        });
        result.TransactionResult.Error.ShouldContain("No permission.");

        {
            // add permission
            await AdminTokenPoolContractStub.AddReleaseController.SendAsync(new ControllerInput
            {
                Address = User1.Address
            });
            var releaseControllers = await AdminTokenPoolContractStub.GetReleaseControllers.CallAsync(new Empty());
            releaseControllers.Addresses.Count.ShouldBe(1);

            // release
            var res = await User1TokenPoolContractStub.SwapToken.SendAsync(new SwapTokenInput
            {
                AmountIn = 10000000,
                AmountOutMin = 5000000,
                Channel = "test",
                Deadline = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 3))),
                To = User1.Address,
                FeeRate = 300,
                Path = { "USDT", "ELF" }
            });
            var swapLog = Swapped.Parser.ParseFrom(res.TransactionResult.Logs
                .First(l => l.Name == nameof(Swapped)).NonIndexed);
            var log = TokenSwapped.Parser.ParseFrom(res.TransactionResult.Logs
                .First(l => l.Name == nameof(TokenSwapped)).NonIndexed);
            log.AmountOut.AmountOut.First().ShouldBe(swapLog.Amounts);
            log.SwapPath.Path.Count.ShouldBe(2);
        }
        {
            var result1 = await User1TokenPoolContractStub.SwapToken.SendWithExceptionAsync(new SwapTokenInput
            {
                AmountIn = 10000000,
                AmountOutMin = 5000000,
                Channel = "test",
                Deadline = Timestamp.FromDateTime(DateTime.UtcNow.Add(new TimeSpan(0, 0, 3))),
                To = User1.Address,
                FeeRate = 522,
                Path = { "USDT", "ELF" }
            });
            result1.TransactionResult.Error.ShouldContain("Swap contract does not exist.");
        }
    }

    [Fact]
    public async Task SetSwapContract_Test()
    {
        await InitTest();
        {
            await AdminTokenPoolContractStub.SetSwapContractAddress.SendAsync(new SetSwapContractAddressInput
            {
                Value =
                {
                    new SwapContract
                    {
                        FeeRate = 300,
                        SwapContractAddress = TestSwapContractAddress
                    }
                }
            });
            var swapContractAddress = await AdminTokenPoolContractStub.GetSwapContracts.CallAsync(new Int64Value
            {
                Value = 300
            });
            swapContractAddress.ShouldBe(TestSwapContractAddress);
        }
        // Repeated address
        {
            await AdminTokenPoolContractStub.SetSwapContractAddress.SendAsync(new SetSwapContractAddressInput
            {
                Value =
                {
                    new SwapContract
                    {
                        FeeRate = 300,
                        SwapContractAddress = TestSwapContractAddress
                    },
                    new SwapContract
                    {
                        FeeRate = 300,
                        SwapContractAddress = User1.Address
                    }
                }
            });
            var swapContractAddress = await AdminTokenPoolContractStub.GetSwapContracts.CallAsync(new Int64Value
            {
                Value = 300
            });
            swapContractAddress.ShouldBe(User1.Address);
        }
        // Fee rate is null
        {
            var result = await AdminTokenPoolContractStub.SetSwapContractAddress.SendWithExceptionAsync(new SetSwapContractAddressInput
            {
                Value =
                {
                    new SwapContract
                    {
                        SwapContractAddress = TestSwapContractAddress
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid fee rate.");
        }
        {
            var result = await AdminTokenPoolContractStub.SetSwapContractAddress.SendWithExceptionAsync(new SetSwapContractAddressInput
            {
                Value =
                {
                    new SwapContract
                    {
                        FeeRate = -1,
                        SwapContractAddress = TestSwapContractAddress
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid fee rate.");
        }
        {
            var result = await AdminTokenPoolContractStub.SetSwapContractAddress.SendWithExceptionAsync(new SetSwapContractAddressInput
            {
                Value =
                {
                    new SwapContract
                    {
                        FeeRate = 10000000,
                        SwapContractAddress = TestSwapContractAddress
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid fee rate.");
        }
        {
            var result = await AdminTokenPoolContractStub.SetSwapContractAddress.SendWithExceptionAsync(new SetSwapContractAddressInput
            {
                Value =
                {
                    new SwapContract
                    {
                        FeeRate = 552
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("Invalid swap contract address.");
        }
        {
            var result = await User1TokenPoolContractStub.SetSwapContractAddress.SendWithExceptionAsync(new SetSwapContractAddressInput
            {
                Value =
                {
                    new SwapContract
                    {
                        FeeRate = 552,
                        SwapContractAddress = TestSwapContractAddress
                    }
                }
            });
            result.TransactionResult.Error.ShouldContain("No permission.");
        }
    }
}