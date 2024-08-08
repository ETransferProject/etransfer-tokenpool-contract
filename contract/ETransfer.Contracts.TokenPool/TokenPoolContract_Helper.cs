using AElf.Contracts.MultiToken;
using AElf.Types;

namespace ETransfer.Contracts.TokenPool
{

    public partial class TokenPoolContract
    {

        private void AssertContractInitialize()
        {
            Assert(State.Initialized.Value, "Contract not Initialized.");
        }

        private void AssertContractAuthor()
        {
            // Initialize by author only
            State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
            var author = State.GenesisContract.GetContractAuthor.Call(Context.Self);
            Assert(author == Context.Sender, "No permission");
        }

        private void AssertAdmin()
        {
            Assert(State.Admin.Value == Context.Sender, "No permission.");
        }

        private void AssertReleaseController()
        {
            Assert((State.ReleaseControllers.Value ?? new ControllerList()).Controllers.Contains(Context.Sender),
                "No permission.");
        }

        private TokenInfo GetTokenInfo(string symbol)
        {
            return State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = symbol
            });
        }

        private void AssertTokenSupport(string symbol)
        {
            Assert(State.TokenPool[symbol] != null, "Symbol not support");
            Assert(State.TokenPool[symbol]?.TokenHolders?.Count > 0, "Empty symbol holder");
        }

        private void AssertMemo(string memo)
        {
            if (string.IsNullOrEmpty(memo)) return;
            var isValid = true;
            foreach (char c in memo)
            {
                if ((c >= '0' && c <='9') || (c >= 'a' && c <='z') || (c >= 'A' && c <='Z')) continue;
                isValid = false;
                break;
            }
            Assert(isValid, "Invalid Memo.");
        }

        private TokenHolder GetTokenHolder(string symbol, Hash virtualHash)
        {
            AssertTokenSupport(symbol);
            foreach (var tokenHolder in State.TokenPool[symbol].TokenHolders)
            {
                if (tokenHolder.VirtualHash == virtualHash) return tokenHolder;
            }
            return null;
        }
        
        private TokenHolder GetTokenHolder(string symbol, Address address)
        {
            AssertTokenSupport(symbol);
            foreach (var tokenHolder in State.TokenPool[symbol].TokenHolders)
            {
                if (tokenHolder.Address == address) return tokenHolder;
            }
            return null;
        }
        
    }
}