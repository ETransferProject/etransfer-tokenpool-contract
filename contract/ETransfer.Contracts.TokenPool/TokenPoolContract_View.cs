using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace ETransfer.Contracts.TokenPool
{
    public partial class TokenPoolContract
    {
        
        public override Address GetAdmin(Empty input)
        {
            return State.Admin.Value;
        }

        public override PoolInfo GetPoolInfo(GetPoolInfoInput input)
        {
            return State.TokenPool[input.Symbol];
        }

        public override TokenSymbolList GetSymbolTokens(Empty input)
        {
            return State.TokenSymbolList.Value;
        }
    }
}