using AElf.CSharp.Core;
using AElf.Sdk.CSharp;

namespace ETransfer.Contracts.TestSwapContracts
{
    public class TestSwapContracts : TestSwapContractsContainer.TestSwapContractsBase
    {
        public override SwapOutput SwapExactTokensForTokens(SwapExactTokensForTokensInput input)
        {
            State.AmountsOut.Value = State.AmountsOut.Value.Add(1);
            Context.Fire(new Swapped
            {
                Amounts = State.AmountsOut.Value
            });
            return new SwapOutput();
        }

        public override GetAmountsOutOutput GetAmountsOut(GetAmountsOutInput input)
        {
            var amounts = State.AmountsOut.Value;
            return new GetAmountsOutOutput
            {
                Amount = { ++amounts }
            };
        }
    }
}

