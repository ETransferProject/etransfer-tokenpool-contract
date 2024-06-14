using System.Collections.Generic;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Boilerplate.TestBase.SmartContractNameProviders;

public class TestSwapContractAddressNameProvider : IContractInitializationProvider
{
    public static readonly Hash Name = HashHelper.ComputeFrom("TestSwapContracts");

    public static readonly string StringName = Name.ToStorageKey();
    public Hash ContractName => Name;
    public string ContractStringName => StringName;
    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        throw new System.NotImplementedException();
    }

    public Hash SystemSmartContractName { get; }
    public string ContractCodeName { get; }
}