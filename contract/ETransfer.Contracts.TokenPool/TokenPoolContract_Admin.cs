using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace ETransfer.Contracts.TokenPool
{
    public partial class TokenPoolContract
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Contract has bean Initialized.");

            // The main chain uses the audit deployment, does not verify the Author
            if (Context.ChainId != MainChainId)
            {
                AssertContractAuthor();
            }

            Assert(input?.TokenSymbolList?.Value?.Count > 0, "Token symbol empty");
            Assert(input != null, "Invalid input");
            if (input.Admin != null)
            {
                Assert(!input.Admin.Value.IsNullOrEmpty(), "Invalid admin address");
            }

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            State.TokenSymbolList.Value = new TokenSymbolList();
            State.Admin.Value = input.Admin ?? Context.Sender;

            foreach (var symbol in input.TokenSymbolList.Value)
            {
                AddTokenPool(symbol);
            }

            State.Initialized.Value = true;
            return new Empty();
        }


        public override Empty AddTokenPool(AddTokenPoolInput input)
        {
            AssertContractInitialize();
            AssertAdmin();
            Assert(State.TokenPool[input.Symbol] == null, "Token pool exists");

            AddTokenPool(input.Symbol);
            return new Empty();
        }

        private void AddTokenPool(string symbol)
        {
            var tokenInfo = GetTokenInfo(symbol);
            Assert(tokenInfo?.Symbol?.Length > 0, "Symbol not exits");
            State.TokenSymbolList.Value.Value.Add(symbol);
            var holderVirtualHash = AddTokenHolder(symbol);

            Context.Fire(new TokenPoolAdded
            {
                Symbol = symbol
            });

            Context.Fire(new TokenHolderAdded
            {
                Symbol = symbol,
                TokenHolders = new TokenHolderList
                {
                    Value =
                    {
                        new TokenHolder
                        {
                            VirtualHash = holderVirtualHash,
                            Address = Context.ConvertVirtualAddressToContractAddress(holderVirtualHash)
                        }
                    }
                }
            });
        }

        public override Empty SetAdmin(Address input)
        {
            AssertContractInitialize();
            AssertAdmin();
            Assert(!input.Value.IsNullOrEmpty(), "Invalid address");

            State.Admin.Value = input;

            return new Empty();
        }

        public override Empty Withdraw(WithdrawInput input)
        {
            AssertContractInitialize();
            AssertAdmin();

            var tokenHolder = GetTokenHolder(input.Symbol, input.VirtualHash);
            Assert(tokenHolder != null, "Token holder not found");

            Context.SendVirtualInline(input.VirtualHash, State.TokenContract.Value,
                nameof(State.TokenContract.Transfer), new AElf.Contracts.MultiToken.TransferInput
                {
                    To = Context.Sender,
                    Symbol = input.Symbol,
                    Amount = input.Amount
                });

            return new Empty();
        }

        public override Empty AddTokenHolders(AddTokenHolderInput input)
        {
            AssertContractInitialize();
            AssertAdmin();
            AssertTokenSupport(input.Symbol);
            Assert(input.HolderCount > 0, "Invalid holder count");
            var totalCount = State.TokenPool[input.Symbol].TokenHolders.Count + input.HolderCount;
            Assert(totalCount <= MaxTokenHolderCount, "Pool holder max count exceeded");

            var tokenHolderAdded = new TokenHolderAdded();
            tokenHolderAdded.Symbol = input.Symbol;
            tokenHolderAdded.TokenHolders = new TokenHolderList();
            for (var i = 0; i < input.HolderCount; i++)
            {
                var virtualHash= AddTokenHolder(input.Symbol);
                tokenHolderAdded.TokenHolders.Value.Add(new TokenHolder
                {
                    VirtualHash = virtualHash,
                    Address = Context.ConvertVirtualAddressToContractAddress(virtualHash)
                });
            }

            Context.Fire(tokenHolderAdded);
            return new Empty();
        }

        private Hash AddTokenHolder(string symbol)
        {
            var virtualHash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(symbol),
                HashHelper.ComputeFrom(State.VirtualHashIndex[symbol]));
            State.VirtualHashIndex[symbol] = State.VirtualHashIndex[symbol].Add(1);
            State.TokenPool[symbol] ??= new PoolInfo
            {
                Symbol = symbol
            };
            State.TokenPool[symbol].TokenHolders.Add(new TokenHolder
            {
                VirtualHash = virtualHash,
                Address = Context.ConvertVirtualAddressToContractAddress(virtualHash)
            });
            return virtualHash;
        }
    }
}