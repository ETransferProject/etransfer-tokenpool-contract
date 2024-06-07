using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Awaken.Contracts.Swap;
using Google.Protobuf.WellKnownTypes;

namespace ETransfer.Contracts.TokenPool;

public partial class TokenPoolContract
{
    private bool IsAddressValid(Address input)
    {
        return input != null && !input.Value.IsNullOrEmpty();
    }

    public override Empty SetSwapContractAddress(SetSwapContractAddressInput input)
    {
        AssertContractInitialize();
        AssertAdmin();
        Assert(input?.Value != null && input.Value.Count > 0, "Invalid input.");
        foreach (var swapContract in input.Value)
        {
            Assert(swapContract.FeeRate > 0 && swapContract.FeeRate <= FeeRateMax, "Invalid fee rate.");
            Assert(IsAddressValid(swapContract.SwapContractAddress), "Invalid swap contract address.");
            State.SwapContractMap[swapContract.FeeRate] = swapContract.SwapContractAddress;
        }

        return new Empty();
    }

    public override Empty SwapToken(SwapTokenInput input)
    {
        AssertContractInitialize();
        AssertReleaseController();
        Assert(input.AmountIn > 0 && input.AmountOutMin > 0, "Invalid amount.");
        Assert(input.Path.Count >= 2, "Invalid path");
        Assert(input.FeeRate > 0 && input.FeeRate <= FeeRateMax, "Invalid fee rate.");
        var symbolIn = input.Path.First();
        var symbolOut = input.Path.Last();
        Assert(State.SwapContractMap[input.FeeRate] != null, "Swap contract does not exist.");
        State.SwapContract.Value = State.SwapContractMap[input.FeeRate];
        var tokenHolder = GetSwapTokenHolder(symbolIn, input.From);
        // Get amounts out
        var amountsOut = GetAmountsOut(input.AmountIn, input.Path);
        // approve
        ApproveToken(tokenHolder.VirtualHash, symbolIn, input.AmountIn, State.SwapContract.Value);
        // swap
        State.SwapContract.SwapExactTokensForTokens.VirtualSend(tokenHolder.VirtualHash,
            new SwapExactTokensForTokensInput
            {
                AmountIn = input.AmountIn,
                AmountOutMin = input.AmountOutMin,
                Path = { input.Path },
                Deadline = input.Deadline,
                Channel = input.Channel,
                To = input.To
            });
        Context.Fire(new TokenSwapped
        {
            SymbolIn = symbolIn,
            SymbolOut = symbolOut,
            To = input.To,
            From = tokenHolder.Address,
            AmountIn = input.AmountIn,
            AmountOut = new AmountsOut
            {
                AmountOut = { amountsOut }
            },
            Channel = input.Channel,
            SwapPath = new SwapPath
            {
                Path = { input.Path }
            }
        });
        return new Empty();
    }

    public override Address GetSwapContracts(Int64Value input)
    {
        return State.SwapContractMap[input.Value];
    }

    private IEnumerable<long> GetAmountsOut(long amountIn, IEnumerable<string> path)
    {
        var amounts = State.SwapContract.GetAmountsOut.Call(new GetAmountsOutInput
        {
            AmountIn = amountIn,
            Path = { path }
        });
        return amounts.Amount;
    }

    private void ApproveToken(Hash holder, string symbolIn, long amountIn, Address spender)
    {
        State.TokenContract.Approve.VirtualSend(holder, new ApproveInput
        {
            Symbol = symbolIn,
            Amount = amountIn,
            Spender = spender
        });
    }

    private TokenHolder GetSwapTokenHolder(string symbolIn, Address from)
    {
        var tokenHolder = GetTokenHolder(symbolIn, from);
        if (tokenHolder != null) return tokenHolder;
        var index = Context.TransactionId.ToInt64() % State.TokenPool[symbolIn].TokenHolders.Count;
        tokenHolder = State.TokenPool[symbolIn].TokenHolders[(int)index];
        return tokenHolder;
    }
}