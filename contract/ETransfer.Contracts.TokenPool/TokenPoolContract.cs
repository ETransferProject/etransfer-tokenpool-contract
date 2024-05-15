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
                Amount = input.Amount 
            });
            
            return new Empty();
        }
    }
}