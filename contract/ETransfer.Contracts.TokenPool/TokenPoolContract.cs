using AElf;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace ETransfer.Contracts.TokenPool
{
    /// <summary>
    /// The C# implementation of the contract defined in token_pool_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public partial class TokenPoolContract : TokenPoolContractContainer.TokenPoolContractBase
    {
        public override Empty TransferToken(TransferTokenInput input)
        {
            AssertContractInitialize();
            
            Assert(input != null, "Invalid input.");
            Assert(input.Symbol?.Length > 0, "Invalid symbol.");
            Assert(input.Amount > 0, "Invalid amount");

            AssertTokenSupport(input.Symbol);
            
            // balance
            var index = Context.TransactionId.ToInt64() % State.TokenPool[input.Symbol].TokenHolders.Count;
            var toAddress = State.TokenPool[input.Symbol].TokenHolders[(int)index].Address;
            
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = toAddress,
                Symbol = input.Symbol,
                Amount = input.Amount
            });
        
            Context.Fire(new TokenPoolTransferred
            {
                From = Context.Sender,
                To = toAddress,
                Symbol = input.Symbol,
                Amount = input.Amount,
                ToChainId = input.ToChainId,
                ToAddress = input.ToAddress,
                MaxEstimateFee = input.MaxEstimateFee
            });
            
            return new Empty();
        }
        
        public override Empty ReleaseToken(ReleaseTokenInput input)
        {
            AssertContractInitialize();
            AssertReleaseController();
            
            Assert(input != null, "Invalid input.");
            Assert(input.Symbol?.Length > 0, "Invalid symbol.");
            Assert(input.Amount > 0, "Invalid amount");
            Assert(input.To != null && !input.To.Value.IsNullOrEmpty(), "Invalid address");
            
            var tokenHolder = GetTokenHolder(input.Symbol, input.From);
            if (tokenHolder == null)
            {
                var index = Context.TransactionId.ToInt64() % State.TokenPool[input.Symbol].TokenHolders.Count;
                tokenHolder = State.TokenPool[input.Symbol].TokenHolders[(int)index];
            }

            State.TokenContract.Transfer.VirtualSend(tokenHolder.VirtualHash, new TransferInput
            {
                To = input.To,
                Symbol = input.Symbol,
                Amount = input.Amount
            });
        
            Context.Fire(new TokenPoolReleased
            {
                From = tokenHolder.Address,
                To = input.To,
                Symbol = input.Symbol,
                Amount = input.Amount 
            });
            
            return new Empty();
        }
    }
}