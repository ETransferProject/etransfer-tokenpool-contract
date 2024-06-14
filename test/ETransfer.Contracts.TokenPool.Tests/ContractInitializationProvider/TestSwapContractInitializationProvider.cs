using System.Collections.Generic;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace ETransfer.Contracts.TokenPool.ContractInitializationProvider;

public class TestSwapContractInitializationProvider : IContractInitializationProvider
{
    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>();
    }

    public Hash SystemSmartContractName { get; } = TestSwapContractAddressNameProvider.Name;
    public string ContractCodeName { get; } = "TestSwapContracts";   
}