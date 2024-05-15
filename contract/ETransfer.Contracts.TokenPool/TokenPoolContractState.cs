using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace ETransfer.Contracts.TokenPool
{

    public partial class TokenPoolContractState : ContractState
    {
        // Whether the contract has been initialized
        public SingletonState<bool> Initialized { get; set; }
        
        // Contract Administrator Address
        public SingletonState<Address> Admin { get; set; }
        
        // List of supported tokens
        public SingletonState<TokenSymbolList> TokenSymbolList { get; set; }
        
        // Token pool
        public MappedState<string, PoolInfo> TokenPool { get; set; }
        
        // Serial number used to generate the virtual hash
        public MappedState<string, int> VirtualHashIndex { get; set; }
    }
}