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
using System.Threading;
using Microsoft.AzureCat.Samples.Framework;
using Microsoft.ServiceFabric.Actors.Runtime;

#endregion

namespace Microsoft.AzureCat.Samples.WorkerActorService
{
    internal static class Program
    {
        /// <summary>
        ///     This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // Create default garbage collection settings for all the actor types
                var settings = new ActorGarbageCollectionSettings(300, 60);

                // These lines register three Actor Services to host your actor classes with the Service Fabric runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see http://aka.ms/servicefabricactorsplatform

                ActorRuntime.RegisterActorAsync<WorkerActor>(
                    (context, actorType) => new ActorService(context,
                        actorType,
                        (s, i) => new WorkerActor(s, i),
                        null,
                        null,
                        new ActorServiceSettings
                        {
                            ActorGarbageCollectionSettings = settings
                        })).GetAwaiter().GetResult();

                ActorRuntime.RegisterActorAsync<QueueActor>(
                    (context, actorType) => new ActorService(context,
                        actorType,
                        (s, i) => new QueueActor(s, i),
                        null,
                        null,
                        new ActorServiceSettings
                        {
                            ActorGarbageCollectionSettings = settings
                        })).GetAwaiter().GetResult();

                ActorRuntime.RegisterActorAsync<ProcessorActor>(
                    (context, actorType) => new ActorService(context,
                        actorType,
                        (s, i) => new ProcessorActor(s, i),
                        null,
                        null,
                        new ActorServiceSettings
                        {
                            ActorGarbageCollectionSettings = settings
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