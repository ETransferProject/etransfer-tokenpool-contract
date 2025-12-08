using AElf.Sdk.CSharp.State;

namespace ETransfer.Contracts.TestSwapContracts;

public class TestSwapContractState : ContractState
{
    public SingletonState<int> AmountsOut { get; set; }

}