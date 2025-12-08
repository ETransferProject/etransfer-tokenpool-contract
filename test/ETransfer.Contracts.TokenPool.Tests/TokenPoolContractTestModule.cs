using System.Collections.Generic;
using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.Boilerplate.TestBase.SmartContractNameProviders;
using AElf.ContractTestBase;
using AElf.Kernel.SmartContract.Application;
using ETransfer.Contracts.TokenPool.ContractInitializationProvider;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace ETransfer.Contracts.TokenPool
{
    [DependsOn(typeof(MainChainDAppContractTestModule))]
    public class TokenPoolContractTestModule : MainChainDAppContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IContractInitializationProvider, TokenPoolContractInitializationProvider>();
            // Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.AddSingleton<IContractInitializationProvider, TestSwapContractInitializationProvider>();

        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            var contractDllLocation = typeof(ETransfer.Contracts.TokenPool.TokenPoolContract).Assembly.Location;
            var contractCodes = new Dictionary<string, byte[]>(contractCodeProvider.Codes)
            {
                {
                    new TokenPoolContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(contractDllLocation)
                },
                {
                    new TestSwapContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(TestSwapContracts.TestSwapContracts).Assembly.Location)
                }
            };
            contractCodeProvider.Codes = contractCodes;
        }
    }
}