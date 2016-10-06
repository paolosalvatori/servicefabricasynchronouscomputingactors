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
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureCat.Samples.Entities;
using Microsoft.AzureCat.Samples.Framework;
using Microsoft.AzureCat.Samples.Framework.Interfaces;
using Microsoft.AzureCat.Samples.WorkerActorService.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

#endregion

namespace Microsoft.AzureCat.Samples.WorkerActorService
{
    /// <remarks>
    /// This actor can be used to start, stop and monitor long running processes.
    /// </remarks>
    [ActorService(Name = "ProcessorActorService")]
    [StatePersistence(StatePersistence.Persisted)]
    internal class ProcessorActor : Actor, IProcessorActor
    {
        #region Private Constants
        private const string DelayProperty = "delay";
        private const string StepsProperty = "steps";
        #endregion

        #region Private Fields
        private Uri queueActorServiceUri;
        private Uri workerActorServiceUri;
        #endregion

        #region Public Constructor
        /// <summary>
        /// Initializes a new instance of ProcessorActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public ProcessorActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }
        #endregion

        #region Actor Overridden Methods
        protected override Task OnActivateAsync()
        {
            base.OnActivateAsync();
            queueActorServiceUri = new Uri($"{ApplicationName}/QueueActorService");
            workerActorServiceUri = new Uri($"{ApplicationName}/WorkerActorService");
            ActorEventSource.Current.Message($"Worker Actor [{Id}] activated.");
            return Task.FromResult(0);
        }

        #endregion

        #region IProcessorActor Methods
        /// <summary>
        /// Starts processing messages from the work queue in a sequential order.
        /// </summary>
        /// <param name="cancellationToken">This CancellationToken is used to stop message processing.</param>
        /// <returns></returns>
        public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                Message message;

                // Creates the proxy to call the queue actor
                var queueActorProxy = ActorProxy.Create<ICircularQueueActor>(new ActorId(Id.ToString()), queueActorServiceUri);

                // Creates the proxy to call the worker actor
                var workerActorProxy = ActorProxy.Create<IWorkerActor>(new ActorId(Id.ToString()), workerActorServiceUri);

                // The method keeps processing messages from the queue, until the queue is empty
                while ((message = await queueActorProxy.DequeueAsync()) != null)
                {
                    try
                    {
                        // Message validation
                        if (string.IsNullOrWhiteSpace(message.MessageId) ||
                            string.IsNullOrWhiteSpace(message.Body))
                        {
                            continue;
                        }

                        // Create delay variable and assign 1 second as default value
                        var delay = TimeSpan.FromSeconds(1);

                        // Create steps variable and assign 10 as default value
                        var steps = 10;

                        if (message.Properties != null)
                        {
                            // Checks if the message Properties collection contains the delay property
                            if (message.Properties.ContainsKey(DelayProperty))
                            {
                                if (message.Properties[DelayProperty] is TimeSpan)
                                {
                                    // Assigns the property value to the delay variable
                                    delay = (TimeSpan)message.Properties[DelayProperty];
                                }
                                else
                                {
                                    var value = message.Properties[DelayProperty] as string;
                                    if (value != null)
                                    {
                                        TimeSpan temp;
                                        if (TimeSpan.TryParse(value, out temp))
                                        {
                                            delay = temp;
                                        }
                                    }
                                }
                            }

                            // Checks if the message Properties collection contains the steps property
                            if (message.Properties.ContainsKey(StepsProperty))
                            {
                                if (message.Properties[StepsProperty] is int)
                                {
                                    // Assigns the property value to the steps variable
                                    steps = (int)message.Properties[StepsProperty];
                                }
                                if (message.Properties[StepsProperty] is long)
                                {
                                    // Assigns the property value to the steps variable
                                    steps = (int)(long)message.Properties[StepsProperty];
                                }
                                else
                                {
                                    var value = message.Properties[StepsProperty] as string;
                                    if (value != null)
                                    {
                                        int temp;
                                        if (int.TryParse(value, out temp))
                                        {
                                            steps = temp;
                                        }
                                    }
                                }
                            }
                        }
                            
                        // NOTE!!!! This section should be replaced by some real computation
                        for (var i = 0; i < steps; i++)
                        {
                            ActorEventSource.Current.Message($"MessageId=[{message.MessageId}] Body=[{message.Body}] ProcessStep=[{i + 1}]");
                            try
                            {
                                await Task.Delay(delay, cancellationToken);
                            }
                            catch (TaskCanceledException)
                            {
                            }

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                continue;
                            }
                            // NOTE: If message processing has been cancelled, 
                            // the method returns immediately without any result
                            ActorEventSource.Current.Message($"MessageId=[{message.MessageId}] elaboration has been canceled and sequential message processing stopped.");
                            return;
                        }
                        ActorEventSource.Current.Message($"MessageId=[{message.MessageId}] has been successfully processed.");

                        for (var n = 1; n <= 3; n++)
                        {
                            try
                            {
                                // Simulates a return value between 1 and 100
                                var random = new Random();
                                var returnValue = random.Next(1, 101);

                                // Returns result to worker actor
                                await workerActorProxy.ReturnSequentialProcessingAsync(message.MessageId, returnValue);

                                //Logs event
                                ActorEventSource.Current.Message($"Sequential processing of MessageId=[{message.MessageId}] ReturnValue=[{returnValue}] successfully returned.");
                                break;
                            }
                            catch (FabricTransientException ex)
                            {
                                ActorEventSource.Current.Message(ex.Message);
                            }
                            catch (AggregateException ex)
                            {
                                foreach (var e in ex.InnerExceptions)
                                {
                                    ActorEventSource.Current.Message(e.Message);
                                }
                            }
                            catch (Exception ex)
                            {
                                ActorEventSource.Current.Message(ex.Message);
                            }
                            Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).Wait(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        ActorEventSource.Current.Error(ex);
                    }
                }

                for (var n = 1; n <= 3; n++)
                {
                    try
                    {
                        // Sets the sequential processing state to false
                        await workerActorProxy.CloseSequentialProcessingAsync(false);
                        ActorEventSource.Current.Message("Closed sequential processing.");
                        return;
                    }
                    catch (FabricTransientException ex)
                    {
                        ActorEventSource.Current.Message(ex.Message);
                    }
                    catch (AggregateException ex)
                    {
                        foreach (var e in ex.InnerExceptions)
                        {
                            ActorEventSource.Current.Message(e.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        ActorEventSource.Current.Message(ex.Message);
                        throw;
                    }
                    Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).Wait(cancellationToken);
                }
                throw new TimeoutException();
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
            }
        }
        #endregion
    }
}
