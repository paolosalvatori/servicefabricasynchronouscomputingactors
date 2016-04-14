// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives



#endregion

namespace Microsoft.AzureCat.Samples.WorkerActorService
{
    using System;
    using System.Threading;
    using Microsoft.AzureCat.Samples.Framework;
    using Microsoft.ServiceFabric.Actors.Runtime;

    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // Create default garbage collection settings for all the actor types
                ActorGarbageCollectionSettings actorGarbageCollectionSettings = new ActorGarbageCollectionSettings(300, 60);

                // These lines register three Actor Services to host your actor classes with the Service Fabric runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see http://aka.ms/servicefabricactorsplatform

                ActorRuntime.RegisterActorAsync<WorkerActor>(
                    (context, actorType) => new ActorService(
                        context,
                        actorType,
                        () => new WorkerActor(),
                        null,
                        new ActorServiceSettings
                        {
                            ActorGarbageCollectionSettings = actorGarbageCollectionSettings
                        })).GetAwaiter().GetResult();

                ActorRuntime.RegisterActorAsync<QueueActor>(
                    (context, actorType) => new ActorService(
                        context,
                        actorType,
                        () => new QueueActor(),
                        null,
                        new ActorServiceSettings
                        {
                            ActorGarbageCollectionSettings = actorGarbageCollectionSettings
                        })).GetAwaiter().GetResult();

                ActorRuntime.RegisterActorAsync<ProcessorActor>(
                    (context, actorType) => new ActorService(
                        context,
                        actorType,
                        () => new ProcessorActor(),
                        null,
                        new ActorServiceSettings
                        {
                            ActorGarbageCollectionSettings = actorGarbageCollectionSettings
                        })).GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e);
                throw;
            }
        }
    }
}