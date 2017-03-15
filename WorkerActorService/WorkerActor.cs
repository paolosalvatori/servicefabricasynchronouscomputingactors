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
using System.Diagnostics;
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
    ///     This actor can be used to start, stop and monitor long running processes.
    /// </remarks>
    [ActorService(Name = "WorkerActorService")]
    [StatePersistence(StatePersistence.Persisted)]
    internal class WorkerActor : Actor, IWorkerActor, IRemindable
    {
        #region Private Constants

        //************************************
        // States
        //************************************
        private const string ReceivedState = "received";
        private const string CompleteState = "complete";
        private const string StoppedState = "aborted";
        private const string MinValueState = "minValue";
        private const string MaxValueState = "maxValue";
        private const string TotValueState = "totValue";
        private const string AvgValueState = "avgValue";
        private const string ResultQueueState = "resultQueue";
        private const string ProcessingState = "state";

        #endregion

        #region Private Fields

        private Uri queueActorServiceUri;
        private Uri processorActorServiceUri;

        #endregion

        #region Public Constructor

        /// <summary>
        ///     Initializes a new instance of WorkerActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public WorkerActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        #endregion

        #region IRemindable Methods

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            try
            {
                // Unregister reminder
                var reminder = GetReminder(reminderName);
                if (reminder != null)
                {
                    await UnregisterReminderAsync(reminder);
                }
                
                // Retieves the cancellation token source from the actor state
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(ProcessingState);
                if (result.HasValue)
                {
                    var cancellationTokenSource = result.Value;
                    // Creates the proxy to call the processor actor
                    var processorActorProxy = ActorProxy.Create<IProcessorActor>(new ActorId(Id.ToString()),
                        processorActorServiceUri);

                    // Tries to start the processor. If the processor is already running, the task will timeout after 1 second.
                    var taskList = new List<Task>
                    {
                        processorActorProxy.ProcessSequentialMessagesAsync(cancellationTokenSource.Token),
                        Task.Delay(TimeSpan.FromSeconds(3), cancellationTokenSource.Token)
                    };
                    await Task.WhenAny(taskList);
                }
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
            }
        }

        #endregion

        #region Actor Overridden Methods

        protected override async Task OnActivateAsync()
        {
            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                await base.OnActivateAsync();

                // Sets actor URIs
                queueActorServiceUri = new Uri($"{ApplicationName}/QueueActorService");
                processorActorServiceUri = new Uri($"{ApplicationName}/ProcessorActorService");

                // Get queue length
                var queueLength = ((WorkerActorService) ActorService).QueueLength;

                // First Activation
                await StateManager.TryAddStateAsync(ReceivedState, (long) 0);
                await StateManager.TryAddStateAsync(CompleteState, (long) 0);
                await StateManager.TryAddStateAsync(StoppedState, (long) 0);
                await StateManager.TryAddStateAsync(MinValueState, long.MaxValue);
                await StateManager.TryAddStateAsync(MaxValueState, long.MinValue);
                await StateManager.TryAddStateAsync(TotValueState, (long) 0);
                await StateManager.TryAddStateAsync(AvgValueState, (double) 0);
                await StateManager.TryAddStateAsync(ResultQueueState, new Queue<Result>(queueLength));

                // Logs event
                ActorEventSource.Current.Message($"Worker Actor [{Id}] activated.");
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                // Logs exception
                ActorEventSource.Current.Error(ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                
                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerOnActivateAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        #endregion

        #region IWorkerActor Methods

        /// <summary>
        ///     Starts processing a message in sequential order. If the message parameter is null,
        ///     the method simply starts the sequential processing loop.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        public async Task<bool> StartSequentialProcessingAsync(Message message)
        {
            // Parameters validation
            if (string.IsNullOrWhiteSpace(message?.MessageId) ||
                string.IsNullOrWhiteSpace(message.Body))
            {
                throw new ArgumentException($"Parameter {nameof(message)} is null or invalid.", nameof(message));
            }

            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Logs event
                ActorEventSource.Current.Message($"Enqueue sequential processing of MessageId=[{message.MessageId}]...");

                // Enqueues the message
                var queueActorProxy = ActorProxy.Create<ICircularQueueActor>(new ActorId(Id.ToString()), queueActorServiceUri);
                await queueActorProxy.EnqueueAsync(message);

                // Traces metric
                ActorEventSource.Current.ReceivedMessage();

                // Logs event
                ActorEventSource.Current.Message($"Sequential processing of MessageId=[{message.MessageId}] successfully enqueued.");

                // Updates internal statistics
                var longResult = await StateManager.TryGetStateAsync<long>(ReceivedState);
                if (longResult.HasValue)
                {
                    await StateManager.SetStateAsync(ReceivedState, longResult.Value + 1);
                }

                // Checks if the sequential process is already running
                // If yes, the method returns immediately.
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(ProcessingState);
                if (result.HasValue)
                {
                    ActorEventSource.Current.Message($"WorkerActor=[{Id}] is already processing messages in a sequential order.");
                    return true;
                }

                // Creates a CancellationTokenSource object to eventually stop the long running task
                var cancellationTokenSource = new CancellationTokenSource();

                // Adds the CancellationTokenSource to the actor state
                await
                    StateManager.TryAddStateAsync(ProcessingState, cancellationTokenSource,
                        cancellationTokenSource.Token);

                //Sets a reminder to return immediately from the call and rememeber to start the processor actor.
                await RegisterReminderAsync(
                    Guid.NewGuid().ToString(),
                    null,
                    TimeSpan.FromMilliseconds(10),
                    TimeSpan.FromMilliseconds(-1));

                return true;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
                return false;
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerStartSequentialProcessingAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        /// <summary>
        ///     Starts processing a message on a separate task.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        public async Task<bool> StartParallelProcessingAsync(Message message)
        {
            // Parameters validation
            if (string.IsNullOrWhiteSpace(message?.MessageId) ||
                string.IsNullOrWhiteSpace(message.Body))
            {
                throw new ArgumentException($"Parameter {nameof(message)} is null or invalid.", nameof(message));
            }

            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Logs event
                ActorEventSource.Current.Message($"Start MessageId=[{message.MessageId}] processing...");

                // Checks if message processing is already running.
                // If yes, the method returns immediately.
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(message.MessageId);
                if (result.HasValue)
                {
                    ActorEventSource.Current.Message(
                        $"WorkerActor=[{Id}] is already processing MessageId=[{message.MessageId}].");
                    return true;
                }

                // Creates a CancellationTokenSource object to eventually stop the long running task
                var cancellationTokenSource = new CancellationTokenSource();

                // Adds the CancellationTokenSource to the actor state using the messageId as name
                await
                    StateManager.TryAddStateAsync(message.MessageId, cancellationTokenSource,
                        cancellationTokenSource.Token);

                // Updates internal statistics
                var longResult = await StateManager.TryGetStateAsync<long>(ReceivedState, cancellationTokenSource.Token);
                if (longResult.HasValue)
                    await StateManager.SetStateAsync(ReceivedState, longResult.Value + 1, cancellationTokenSource.Token);

                var actorProxy = ActorProxy.Create<IProcessorActor>(new ActorId(message.MessageId),
                    processorActorServiceUri);

                // Starts the message processing
                await actorProxy.ProcessParallelMessagesAsync(Id.ToString(), message, cancellationTokenSource.Token);

                // Logs event
                ActorEventSource.Current.Message($"Parallel processing of MessageId=[{message.MessageId}] successfully started.");

                // Traces metric
                ActorEventSource.Current.ReceivedMessage();

                return true;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
                return false;
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerStartParallelProcessingAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        /// <summary>
        ///     Stops the sequential processing task.
        /// </summary>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        public async Task<bool> StopSequentialProcessingAsync()
        {
            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Logs event
                ActorEventSource.Current.Message("Stopping sequential processing...");

                // Retrieves the CancellationTokenSource from the actor state 
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(ProcessingState);
                if (!result.HasValue)
                    return false;

                // Cancels the message processing task by invoking the CancellationTokenSource.Cancel method
                result.Value.Cancel();

                // Removes the CancellationTokenSource from the actor state
                var ok = await StateManager.TryRemoveStateAsync(ProcessingState);

                // Updates internal statistics
                var longResult = await StateManager.TryGetStateAsync<long>(StoppedState);
                if (longResult.HasValue)
                    await StateManager.SetStateAsync(StoppedState, longResult.Value + 1);

                // Logs event
                ActorEventSource.Current.Message(ok ? "Sequential processing successfully stopped."
                                                    : "Sequential processing failed to stop.");

                // Traces metric
                ActorEventSource.Current.StoppedMessage();

                return true;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
                return false;
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerStopSequentialProcessingAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        /// <summary>
        ///     Stops the elaboration of a specific message identified by its id.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        public async Task<bool> StopParallelProcessingAsync(string messageId)
        {
            // Parameters validation
            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException($"Parameter {nameof(messageId)} cannot be null or empty.", nameof(messageId));
            }

            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Logs event
                ActorEventSource.Current.Message($"Stopping parallel processing of MessageId=[{messageId}]...");

                // Retrieves the CancellationTokenSource from the actor state 
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(messageId);
                if (!result.HasValue)
                    return false;

                // Cancels the message processing task by invoking the CancellationTokenSource.Cancel method
                result.Value.Cancel();

                // Removes the CancellationTokenSource from the actor state
                var ok = await StateManager.TryRemoveStateAsync(messageId);

                // Updates internal statistics
                var longResult = await StateManager.TryGetStateAsync<long>(StoppedState);
                if (longResult.HasValue)
                    await StateManager.SetStateAsync(StoppedState, longResult.Value + 1);

                // Logs event
                ActorEventSource.Current.Message(ok ? $"Parallel processing of MessageId=[{messageId}] successfully stopped."
                                                    : $"Parallel processing of MessageId=[{messageId}] failed to stop.");

                // Traces metric
                ActorEventSource.Current.StoppedMessage();

                return true;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
                return false;
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerStopParallelProcessingAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        /// <summary>
        ///     Used by the sequential processing task to signal the completion
        ///     of a message processing and return computed results.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="returnValue">The message processing result.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        public async Task<bool> ReturnSequentialProcessingAsync(string messageId, long returnValue)
        {
            // Parameters validation
            if (string.IsNullOrWhiteSpace(messageId))
            {
                messageId = "UNKNOWN";
            }

            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Traces metric
                ActorEventSource.Current.ProcessedMessage();

                // Logs event
                ActorEventSource.Current.Message($"Returning sequential processing of MessageId=[{messageId}] ReturnValue=[{returnValue}]...");

                // Raises event
                var ev = GetEvent<IWorkerActorEvents>();
                ev.MessageProcessingCompleted(messageId, returnValue);

                // Updates internal statistics
                var ok = true;
                var longResult = await StateManager.TryGetStateAsync<long>(CompleteState);
                if (longResult.HasValue)
                {
                    var complete = longResult.Value + 1;
                    await StateManager.SetStateAsync(CompleteState, complete);
                    longResult = await StateManager.TryGetStateAsync<long>(MinValueState);
                    if (longResult.HasValue && (returnValue < longResult.Value))
                        await StateManager.SetStateAsync(MinValueState, returnValue);
                    longResult = await StateManager.TryGetStateAsync<long>(MaxValueState);
                    if (longResult.HasValue && (returnValue > longResult.Value))
                        await StateManager.SetStateAsync(MaxValueState, returnValue);
                    longResult = await StateManager.TryGetStateAsync<long>(TotValueState);
                    if (longResult.HasValue)
                    {
                        var totValue = longResult.Value + returnValue;
                        await StateManager.SetStateAsync(TotValueState, totValue);
                        await StateManager.SetStateAsync(AvgValueState, (double) totValue/complete);
                    }
                    else
                    {
                        ok = false;
                    }
                    var queueResult = await StateManager.TryGetStateAsync<Queue<Result>>(ResultQueueState);
                    if (queueResult.HasValue && (queueResult.Value != null))
                    {
                        var queue = queueResult.Value;

                        // Enqueues the latest result
                        queue.Enqueue(
                            new Result
                            {
                                MessageId = messageId,
                                ReturnValue = returnValue
                            });

                        // Get queue length
                        var queueLength = ((WorkerActorService) ActorService).QueueLength;

                        // The actor keeps the latest n payloads in a queue, where n is  
                        // defined by the QueueLength parameter in the Settings.xml file.
                        if (queue.Count > queueLength)
                            queue.Dequeue();

                        // Saves the result queue
                        await StateManager.SetStateAsync(ResultQueueState, queue);
                    }
                    else
                    {
                        ok = false;
                    }
                }
                else
                {
                    ok = false;
                }

                // Logs event
                ActorEventSource.Current.Message(ok ? $"Sequential processing of MessageId=[{messageId}] ReturnValue=[{returnValue}] successfully returned."
                                                    : $"Sequential processing of MessageId=[{messageId}] ReturnValue=[{returnValue}] failed to store result.");

                return true;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
                return false;
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerReturnSequentialProcessingAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        /// <summary>
        ///     Used by the parallel processing task to signal the completion
        ///     of a message processing and return computed results.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="returnValue">The message processing result.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        public async Task<bool> ReturnParallelProcessingAsync(string messageId, long returnValue)
        {
            // Parameters validation
            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException($"Parameter {nameof(messageId)} cannot be null or empty.", nameof(messageId));
            }

            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Traces metric
                ActorEventSource.Current.ProcessedMessage();

                ActorEventSource.Current.Message($"Returning parallel processing of MessageId=[{messageId}] ReturnValue=[{returnValue}]...");

                // Raises event
                var ev = GetEvent<IWorkerActorEvents>();
                ev.MessageProcessingCompleted(messageId, returnValue);

                // Retrieves the CancellationTokenSource from the actor state 
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(messageId);
                if (!result.HasValue)
                {
                    return false;
                }
                // Cancels the message processing task by invoking the CancellationTokenSource.Cancel method
                result.Value.Cancel();

                // Removes the CancellationTokenSource from the actor state
                var ok = await StateManager.TryRemoveStateAsync(messageId);

                // Updates internal statistics
                var longResult = await StateManager.TryGetStateAsync<long>(CompleteState);
                if (longResult.HasValue)
                {
                    var complete = longResult.Value + 1;
                    await StateManager.SetStateAsync(CompleteState, complete);
                    if (longResult.HasValue && (returnValue < longResult.Value))
                        await StateManager.SetStateAsync(MinValueState, returnValue);
                    longResult = await StateManager.TryGetStateAsync<long>(MaxValueState);
                    if (longResult.HasValue && (returnValue > longResult.Value))
                        await StateManager.SetStateAsync(MaxValueState, returnValue);
                    longResult = await StateManager.TryGetStateAsync<long>(TotValueState);
                    if (longResult.HasValue)
                    {
                        var totValue = longResult.Value + returnValue;
                        await StateManager.SetStateAsync(TotValueState, totValue);
                        await StateManager.SetStateAsync(AvgValueState, (double) totValue/complete);
                    }
                    else
                    {
                        ok = false;
                    }
                    var queueResult = await StateManager.TryGetStateAsync<Queue<Result>>(ResultQueueState);
                    if (queueResult.HasValue && (queueResult.Value != null))
                    {
                        var queue = queueResult.Value;

                        // Enqueues the latest result
                        queue.Enqueue(
                            new Result
                            {
                                MessageId = messageId,
                                ReturnValue = returnValue
                            });

                        // Get queue length
                        var queueLength = ((WorkerActorService)ActorService).QueueLength;

                        // The actor keeps the latest n payloads in a queue, where n is  
                        // defined by the QueueLength parameter in the Settings.xml file.
                        if (queue.Count > queueLength)
                            queue.Dequeue();

                        // Saves the result queue
                        await StateManager.SetStateAsync(ResultQueueState, queue);

                        // Delete the processor actor
                        var actorId = new ActorId(messageId);
                        var actorServiceProxy = ActorServiceProxy.Create(processorActorServiceUri, actorId);
                        var cancellationTokenSource = new CancellationTokenSource();
                        await actorServiceProxy.DeleteActorAsync(actorId, cancellationTokenSource.Token);
                    }
                    else
                    {
                        ok = false;
                    }
                }
                else
                {
                    ok = false;
                }

                // Logs event
                ActorEventSource.Current.Message(ok ? $"Parallel processing of MessageId=[{messageId}] ReturnValue=[{returnValue}] successfully returned."
                                                    : $"Parallel processing of MessageId=[{messageId}] ReturnValue=[{returnValue}] failed to store result.");

                return true;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
                return false;
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerReturnParallelProcessingAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        /// <summary>
        ///     Checks if the sequential processing task is running.
        /// </summary>
        /// <returns>True if sequential processing task is still running, false otherwise.</returns>
        public async Task<bool> IsSequentialProcessingRunningAsync()
        {
            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Retrieves processing state
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(ProcessingState);
                var value = result.HasValue;

                // Logs event
                ActorEventSource.Current.Message($"ProcessingState=[{value}]");

                return value;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
                return false;
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerIsSequentialProcessingRunningAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        /// <summary>
        ///     Checks if the elaboration of a given message is running.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <returns>True if the elaboration of the message is still running, false otherwise.</returns>
        public async Task<bool> IsParallelProcessingRunningAsync(string messageId)
        {
            // Parameters validation
            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException($"Parameter {nameof(messageId)} cannot be null or empty.", nameof(messageId));
            }

            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Retrieves the CancellationTokenSource from the actor state 
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(messageId);

                // Logs event
                ActorEventSource.Current.Message($"MessageId=[{messageId}] ProcessingState=[{result.HasValue}]");

                return result.HasValue;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
                return false;
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerIsParallelProcessingRunningAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        /// <summary>
        ///     Closes sequential processing by removing the corresponding cancellation token source.
        /// </summary>
        /// <param name="runningState">True if the sequential processing task is still running, false otherwise.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        public async Task<bool> CloseSequentialProcessingAsync(bool runningState)
        {
            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Logs event
                ActorEventSource.Current.Message("Closing sequential processing...");

                // Retrieves the CancellationTokenSource from the actor state 
                var result = await StateManager.TryGetStateAsync<CancellationTokenSource>(ProcessingState);
                if (!result.HasValue)
                {
                    return false;
                }

                // Removes the CancellationTokenSource from the actor state
                await StateManager.TryRemoveStateAsync(ProcessingState);

                // Logs event
                ActorEventSource.Current.Message("Sequential processing closed.");

                return true;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
                return false;
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerCloseSequentialProcessingAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
        }

        /// <summary>
        ///     Gets the worker actor statistics from its internal state.
        /// </summary>
        /// <returns>True if sequential processing task is still running, false otherwise.</returns>
        public async Task<Statistics> GetProcessingStatisticsAsync()
        {
            var stopwatch = new Stopwatch();
            var isSuccess = true;

            try
            {
                // Starts stopwatch 
                stopwatch.Start();

                // Creates statistics object
                var statistics = new Statistics();

                // Retrieves received messages
                var longResult = await StateManager.TryGetStateAsync<long>(ReceivedState);
                var ok = longResult.HasValue;
                if (longResult.HasValue)
                    statistics.Received = longResult.Value;

                // Retrieves complete messages
                longResult = await StateManager.TryGetStateAsync<long>(CompleteState);
                ok = ok && longResult.HasValue;
                if (longResult.HasValue)
                    statistics.Complete = longResult.Value;

                // Retrieves aborted messages
                longResult = await StateManager.TryGetStateAsync<long>(StoppedState);
                ok = ok && longResult.HasValue;
                if (longResult.HasValue)
                    statistics.Stopped = longResult.Value;

                // Retrieves min value
                longResult = await StateManager.TryGetStateAsync<long>(MinValueState);
                ok = ok && longResult.HasValue;
                if (longResult.HasValue)
                    statistics.MinValue = longResult.Value;

                // Retrieves max value
                longResult = await StateManager.TryGetStateAsync<long>(MaxValueState);
                ok = ok && longResult.HasValue;
                if (longResult.HasValue)
                    statistics.MaxValue = longResult.Value;

                // Retrieves tot value
                longResult = await StateManager.TryGetStateAsync<long>(TotValueState);
                ok = ok && longResult.HasValue;
                if (longResult.HasValue)
                    statistics.TotalValue = longResult.Value;

                // Retrieves avg value
                var doubleResult = await StateManager.TryGetStateAsync<double>(AvgValueState);
                ok = ok && doubleResult.HasValue;
                if (doubleResult.HasValue)
                    statistics.AverageValue = doubleResult.Value;

                // Retrieves latest N results
                var queueResult = await StateManager.TryGetStateAsync<Queue<Result>>(ResultQueueState);
                ok = ok && queueResult.HasValue;
                if (queueResult.HasValue && (queueResult.Value != null))
                    statistics.Results = queueResult.Value.ToArray();

                // Logs event
                ActorEventSource.Current.Message(
                    ok
                        ? "Successfully returned all statistics."
                        : "Failed to return all statistics");

                return statistics;
            }
            catch (Exception ex)
            {
                // Sets success flag to false
                isSuccess = false;

                ActorEventSource.Current.Error(ex);
            }
            finally
            {
                stopwatch.Stop();

                // Logs method duration
                ActorEventSource.Current.RequestComplete("WorkerCloseSequentialProcessingAsync",
                                                 isSuccess,
                                                 stopwatch.ElapsedMilliseconds,
                                                 isSuccess ? "Succeded" : "Failed");
            }
            return null;
        }

        #endregion
    }
}