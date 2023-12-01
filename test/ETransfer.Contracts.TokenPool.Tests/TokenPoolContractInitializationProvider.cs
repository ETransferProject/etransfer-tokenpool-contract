using System.Collections.Generic;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace ETransfer.Contracts.TokenPool
{
    public class TokenPoolContractInitializationProvider : IContractInitializationProvider
    {
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }

        public Hash SystemSmartContractName { get; } = TokenPoolContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "ETransfer.Contracts.TokenPool";
    }
}