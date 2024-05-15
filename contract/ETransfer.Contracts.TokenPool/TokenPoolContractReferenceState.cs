using AElf.Contracts.MultiToken;
using AElf.Standards.ACS0;

namespace ETransfer.Contracts.TokenPool
{

    public partial class TokenPoolContractState
    {
        
        internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

    }
}