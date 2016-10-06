// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives

#endregion

using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace Microsoft.AzureCat.Samples.GatewayService
{
    /// <summary>
    ///     The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class GatewayService : StatelessService
    {
        #region Public Constructor

        public GatewayService(StatelessServiceContext context)
            : base(context)
        {
        }

        #endregion

        #region Private Methods

        private void ReadSettings()
        {
            try
            {
                // Set default values
                serviceRelativePath = DefaultServiceRelativePath;
                maxRetryCount = DefaultMaxRetryCount;
                backoffDelay = DefaultBackoffDelay;

                // Read settings from the DeviceActorServiceConfig section in the Settings.xml file
                var activationContext = Context.CodePackageActivationContext;
                var config = activationContext.GetConfigurationPackageObject(ConfigurationPackage);
                var section = config.Settings.Sections[ConfigurationSection];

                // Read the ServiceRelativePath setting from the Settings.xml file
                if (section.Parameters.Any(
                    p => string.Compare(
                             p.Name,
                             ServiceRelativePathParameter,
                             StringComparison.InvariantCultureIgnoreCase) == 0))
                {
                    var parameter = section.Parameters[ServiceRelativePathParameter];
                    if (!string.IsNullOrWhiteSpace(parameter?.Value))
                        serviceRelativePath = parameter.Value;
                }

                // Read the MaxQueryRetryCount setting from the Settings.xml file
                if (section.Parameters.Any(
                    p => string.Compare(
                             p.Name,
                             MaxQueryRetryCountParameter,
                             StringComparison.InvariantCultureIgnoreCase) == 0))
                {
                    var parameter = section.Parameters[MaxQueryRetryCountParameter];
                    if (!string.IsNullOrWhiteSpace(parameter?.Value))
                        int.TryParse(parameter.Value, out maxRetryCount);
                }

                // Read the BackoffDelay setting from the Settings.xml file
                if (section.Parameters.Any(
                    p => string.Compare(
                             p.Name,
                             BackoffDelayParameter,
                             StringComparison.InvariantCultureIgnoreCase) == 0))
                {
                    var parameter = section.Parameters[BackoffDelayParameter];
                    if (!string.IsNullOrWhiteSpace(parameter?.Value))
                        int.TryParse(parameter.Value, out backoffDelay);
                }
            }
            catch (KeyNotFoundException ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
            }
            ServiceEventSource.Current.Message($"[{ServiceRelativePathParameter}] = [{serviceRelativePath}]");
            ServiceEventSource.Current.Message($"[{MaxQueryRetryCountParameter}] = [{maxRetryCount}]");
            ServiceEventSource.Current.Message($"[{BackoffDelayParameter}] = [{backoffDelay} milliseconds]");
        }

        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************
        private const string ConfigurationPackage = "Config";
        private const string ConfigurationSection = "GatewayServiceConfig";
        private const string ServiceRelativePathParameter = "ServiceRelativePath";
        private const string MaxQueryRetryCountParameter = "MaxRetryCount";
        private const string BackoffDelayParameter = "BackoffDelay";

        //************************************
        // Formats & Messages
        //************************************
        private const string RetryTimeoutExhausted = "Retry timeout exhausted.";

        //************************************
        // Constants
        //************************************
        private const string DefaultServiceRelativePath = "worker";
        private const int DefaultMaxRetryCount = 3;
        private const int DefaultBackoffDelay = 1000;

        #endregion

        #region Private Fields

        private string serviceRelativePath;
        private int maxRetryCount;
        private int backoffDelay;

        #endregion

        #region StatelessService Protected Methods

        /// <summary>
        ///     Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            ReadSettings();
            for (var k = 1; k <= maxRetryCount; k++)
            {
                try
                {
                    return new[]
                    {
                        new ServiceInstanceListener(
                            s => new OwinCommunicationListener(serviceRelativePath, new Startup(), s))
                    };
                }
                catch (FabricTransientException ex)
                {
                    ServiceEventSource.Current.Message(ex.Message);
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                        ServiceEventSource.Current.Message(e.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.Message(ex.Message);
                    throw;
                }
                Task.Delay(backoffDelay).Wait();
            }
            throw new TimeoutException(RetryTimeoutExhausted);
        }

        /// <summary>
        ///     This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancelServiceInstance">Canceled when Service Fabric terminates this instance.</param>
        protected override async Task RunAsync(CancellationToken cancelServiceInstance)
        {
            // This service instance continues processing until the instance is terminated.
            while (!cancelServiceInstance.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(1), cancelServiceInstance);
        }

        #endregion
    }
}