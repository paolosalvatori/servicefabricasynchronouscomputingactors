// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives

#endregion

using System;
using System.Fabric;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureCat.Samples.Framework;
using Microsoft.Owin.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace Microsoft.AzureCat.Samples.GatewayService
{
    public class OwinCommunicationListener : ICommunicationListener
    {
        #region Public Constructor

        public OwinCommunicationListener(string appRoot, IOwinAppBuilder startup, StatelessServiceContext context)
        {
            this.startup = startup;
            this.appRoot = appRoot;
            this.context = context;
        }

        #endregion

        #region Public Static Properties

        public static string WorkerActorServiceUri { get; private set; }

        #endregion

        #region Private Methods

        private void StopWebServer()
        {
            if (serverHandle == null)
                return;
            try
            {
                serverHandle.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // no-op
            }
        }

        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************
        private const string ConfigurationPackage = "Config";
        private const string ConfigurationSection = "GatewayServiceConfig";
        private const string DeviceActorServiceUriParameter = "WorkerActorServiceUri";

        #endregion

        #region Private Fields

        private readonly IOwinAppBuilder startup;
        private readonly string appRoot;
        private readonly StatelessServiceContext context;
        private IDisposable serverHandle;
        private string listeningAddress;

        #endregion

        #region ICommunicationListener Methods

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Read settings from the DeviceActorServiceConfig section in the Settings.xml file
                var activationContext = context.CodePackageActivationContext;
                var config = activationContext.GetConfigurationPackageObject(ConfigurationPackage);
                var section = config.Settings.Sections[ConfigurationSection];

                // Check if a parameter called WorkerActorServiceUri exists in the DeviceActorServiceConfig config section
                if (section.Parameters.Any(
                    p => string.Compare(
                             p.Name,
                             DeviceActorServiceUriParameter,
                             StringComparison.InvariantCultureIgnoreCase) == 0))
                {
                    var parameter = section.Parameters[DeviceActorServiceUriParameter];
                    WorkerActorServiceUri = !string.IsNullOrWhiteSpace(parameter?.Value)
                        ? parameter.Value
                        :
                        // By default, the current service assumes that if no URI is explicitly defined for the actor service
                        // in the Setting.xml file, the latter is hosted in the same Service Fabric application.
                        $"fabric:/{context.ServiceName.Segments[1]}WorkerActorService";
                }
                else
                {
                    // By default, the current service assumes that if no URI is explicitly defined for the actor service
                    // in the Setting.xml file, the latter is hosted in the same Service Fabric application.
                    WorkerActorServiceUri = $"fabric:/{context.ServiceName.Segments[1]}WorkerActorService";
                }

                var serviceEndpoint = context.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
                var port = serviceEndpoint.Port;

                listeningAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}",
                    port,
                    string.IsNullOrWhiteSpace(appRoot)
                        ? string.Empty
                        : appRoot.TrimEnd('/') + '/');

                serverHandle = WebApp.Start(listeningAddress, appBuilder => startup.Configuration(appBuilder));
                var publishAddress = listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

                ServiceEventSource.Current.Message($"Listening on [{publishAddress}]");

                return Task.FromResult(publishAddress);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.Message("Close");

            StopWebServer();

            return Task.FromResult(true);
        }

        public void Abort()
        {
            ServiceEventSource.Current.Message("Abort");

            StopWebServer();
        }

        #endregion
    }
}