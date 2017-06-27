#region Copyright

//=======================================================================================
// Microsoft Azure Customer Advisory Team  
//
// This sample is supplemental to the technical guidance published on the community
// blog at https://github.com/paolosalvatori. 
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
using System.Fabric;
using System.Linq;
using Microsoft.AzureCat.Samples.Framework;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

#endregion

namespace Microsoft.AzureCat.Samples.WorkerActorService
{
    public class WorkerActorService : ActorService
    {
        #region Private Constants

        //************************************
        // Parameters
        //************************************
        private const string ConfigurationPackage = "Config";
        private const string ConfigurationSection = "WorkerActorCustomConfig";
        private const string QueueLengthParameter = "QueueLength";

        //************************************
        // Constants
        //************************************
        private const int DefaultQueueLength = 100;

        #endregion

        #region Public Constructor

        public WorkerActorService(StatefulServiceContext context,
            ActorTypeInformation typeInfo,
            Func<ActorService, ActorId, ActorBase> actorFactory,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory,
            IActorStateProvider stateProvider = null,
            ActorServiceSettings settings = null)
            : base(context, typeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
            // Read Settings
            ReadSettings(context.CodePackageActivationContext.GetConfigurationPackageObject(ConfigurationPackage));

            // Creates event handlers for configuration changes
            context.CodePackageActivationContext.ConfigurationPackageAddedEvent +=
                CodePackageActivationContext_ConfigurationPackageAddedEvent;
            context.CodePackageActivationContext.ConfigurationPackageModifiedEvent +=
                CodePackageActivationContext_ConfigurationPackageModifiedEvent;
            context.CodePackageActivationContext.ConfigurationPackageRemovedEvent +=
                CodePackageActivationContext_ConfigurationPackageRemovedEvent;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the queue length
        /// </summary>
        public int QueueLength { get; set; }

        #endregion

        #region Private Methods

        private void CodePackageActivationContext_ConfigurationPackageAddedEvent(object sender,
            PackageAddedEventArgs<ConfigurationPackage> e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            ReadSettings(e.Package);
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender,
            PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            ReadSettings(e.NewPackage);
        }

        private void CodePackageActivationContext_ConfigurationPackageRemovedEvent(object sender,
            PackageRemovedEventArgs<ConfigurationPackage> e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            ReadSettings(e.Package);
        }

        private void ReadSettings(ConfigurationPackage configurationPackage)
        {
            try
            {
                if (configurationPackage == null)
                {
                    throw new ArgumentException(
                        $"The ConfigurationPackage of the [{Context.ServiceName}-{Context.ReplicaId}] service replica is null.");
                }

                if (configurationPackage.Settings == null ||
                    !configurationPackage.Settings.Sections.Contains(ConfigurationSection))
                {
                    throw new ArgumentException(
                        $"The ConfigurationPackage of the [{Context.ServiceName}-{Context.ReplicaId}] service replica does not contain any configuration section called [{ConfigurationSection}].");
                }

                var section = configurationPackage.Settings.Sections[ConfigurationSection];

                // Check if a parameter called QueueLength exists in the ActorConfig config section
                if (section.Parameters.Any(
                    p => string.Compare(
                             p.Name,
                             QueueLengthParameter,
                             StringComparison.InvariantCultureIgnoreCase) == 0))
                {
                    int queueLength;
                    var parameter = section.Parameters[QueueLengthParameter];
                    if (!string.IsNullOrWhiteSpace(parameter.Value) &&
                        int.TryParse(parameter.Value, out queueLength))
                    {
                        QueueLength = queueLength;
                    }
                    else
                    {
                        QueueLength = DefaultQueueLength;
                    }
                }
                
                // Logs event
                ServiceEventSource.Current.Message($"[{QueueLengthParameter}] = [{QueueLength}]");
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Message(ex.Message);
                throw;
            }
        }
        #endregion
    }
}