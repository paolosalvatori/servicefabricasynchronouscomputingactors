#region Copyright

//=======================================================================================
// Microsoft Azure Customer Advisory Team  
//
// This sample is supplemental to the technical guidance published on the community
// blog at http://blogs.msdn.com/b/paolos/. 
// 
// Author: Paolo Salvatori
//=======================================================================================
// Copyright © 2016 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
//=======================================================================================

#endregion

#region Using Directives

using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureCat.Samples.Framework;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.ServiceFabric;

#endregion

namespace Microsoft.AzureCat.Samples.EventCollectorService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class EventCollectorService : StatelessService
    {
        #region Private Static Fields

        // ReSharper disable once NotAccessedField.Local
        private static DiagnosticPipeline diagnosticsPipeline;

        #endregion

        #region Public Constructor

        public EventCollectorService(StatelessServiceContext context) : base(context)
        {
            try
            {
                // Create event handlers for configuration package changes
                context.CodePackageActivationContext.ConfigurationPackageAddedEvent += CodePackageActivationContext_ConfigurationPackageAddedEvent;
                context.CodePackageActivationContext.ConfigurationPackageModifiedEvent += CodePackageActivationContext_ConfigurationPackageModifiedEvent;
                context.CodePackageActivationContext.ConfigurationPackageRemovedEvent += CodePackageActivationContext_ConfigurationPackageRemovedEvent;
            }
            catch (Exception ex)
            {
                // Log exception
                ServiceEventSource.Current.Error(ex);
            }
        }

        #endregion

        #region Event Handlers

        private void CodePackageActivationContext_ConfigurationPackageRemovedEvent(object sender, PackageRemovedEventArgs<ConfigurationPackage> e)
        {
            try
            {
                // Create diagnostic pipeline
                diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("LongRunningActors-EventCollectorService-DiagnosticsPipeline");
                
                // Log success
                ServiceEventSource.Current.Message("Diagnostics Pipeline successfully created.");
            }
            catch (Exception ex)
            {
                // Log exception
                ServiceEventSource.Current.Error(ex);
            }
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            try
            {
                // Create diagnostic pipeline
                diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("LongRunningActors-EventCollectorService-DiagnosticsPipeline");

                // Log success
                ServiceEventSource.Current.Message("Diagnostics Pipeline successfully created.");
            }
            catch (Exception ex)
            {
                // Log exception
                ServiceEventSource.Current.Error(ex);
            }
        }

        private void CodePackageActivationContext_ConfigurationPackageAddedEvent(object sender, PackageAddedEventArgs<ConfigurationPackage> e)
        {
            try
            {
                // Create diagnostic pipeline
                diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("LongRunningActors-EventCollectorService-DiagnosticsPipeline");

                // Log success
                ServiceEventSource.Current.Message("Diagnostics Pipeline successfully created.");
            }
            catch (Exception ex)
            {
                // Log exception
                ServiceEventSource.Current.Error(ex);
            }
        }

        #endregion

        #region StatelessService Overridden Methods

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// Notification that the service is being aborted. RunAsync may be running concurrently 
        /// with the execution of this method, as cancellation is not awaited on the abort path.
        /// </summary>
        protected override void OnAbort()
        {
            try
            {
                diagnosticsPipeline?.Dispose();
            }
            catch (Exception ex)
            {
                // Log exception
                ServiceEventSource.Current.Error(ex);
            }
        }

        /// <summary>
        /// This method is called as the final step of closing the service. Override this method 
        /// to be notified that Close has completed for this instance's internal components.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                diagnosticsPipeline?.Dispose();
            }
            catch (Exception ex)
            {
                // Log exception
                ServiceEventSource.Current.Error(ex);
            }
            return Task.FromResult(true);
        }


        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (diagnosticsPipeline == null)
                    {
                        try
                        {
                            // Create diagnostic pipeline
                            diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("LongRunningActors-EventCollectorService-DiagnosticsPipeline");

                            // Log success
                            ServiceEventSource.Current.Message("Diagnostics Pipeline successfully created.");
                        }
                        catch (Exception ex)
                        {
                            // Log exception
                            ServiceEventSource.Current.Error(ex);
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
                // ReSharper disable once FunctionNeverReturns
            }
            finally
            {
                try
                {
                    diagnosticsPipeline?.Dispose();
                }
                catch (Exception ex)
                {
                    // Log exception
                    ServiceEventSource.Current.Error(ex);
                }
            }
        } 
        #endregion
    }
}
