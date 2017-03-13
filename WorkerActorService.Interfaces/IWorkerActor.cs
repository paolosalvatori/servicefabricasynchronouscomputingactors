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

using System.Threading.Tasks;
using Microsoft.AzureCat.Samples.Entities;
using Microsoft.ServiceFabric.Actors;

#endregion

namespace Microsoft.AzureCat.Samples.WorkerActorService.Interfaces
{
    /// <summary>
    ///     This interface represents the actions a client app can perform on an actor.
    ///     It MUST derive from IActor and all methods MUST return a Task.
    /// </summary>
    public interface IWorkerActor : IActor, IActorEventPublisher<IWorkerActorEvents>
    {
        /// <summary>
        ///     Starts processing a message in sequential order. If the message parameter is null,
        ///     the method simply starts the sequential processing loop.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        Task<bool> StartSequentialProcessingAsync(Message message);

        /// <summary>
        ///     Starts processing a message on a separate task.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        Task<bool> StartParallelProcessingAsync(Message message);

        /// <summary>
        ///     Stops the sequential processing task.
        /// </summary>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        Task<bool> StopSequentialProcessingAsync();

        /// <summary>
        ///     Stops the elaboration of a specific message identified by its id.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        Task<bool> StopParallelProcessingAsync(string messageId);

        /// <summary>
        ///     Used by the sequential processing task to signal the completion
        ///     of a message processing and return computed results.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="returnValue">The message processing result.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        Task<bool> ReturnSequentialProcessingAsync(string messageId, long returnValue);

        /// <summary>
        ///     Used by the parallel processing task to signal the completion
        ///     of a message processing and return computed results.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="returnValue">The message processing result.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        Task<bool> ReturnParallelProcessingAsync(string messageId, long returnValue);

        /// <summary>
        ///     Checks if the sequential processing task is running.
        /// </summary>
        /// <returns>True if sequential processing task is still running, false otherwise.</returns>
        Task<bool> IsSequentialProcessingRunningAsync();

        /// <summary>
        ///     Checks if the elaboration of a given message is running.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <returns>True if the elaboration of the message is still running, false otherwise.</returns>
        Task<bool> IsParallelProcessingRunningAsync(string messageId);

        /// <summary>
        ///     Sets sequential processing state.
        /// </summary>
        /// <param name="runningState">True if the sequential processing task is still running, false otherwise.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        Task<bool> CloseSequentialProcessingAsync(bool runningState);

        /// <summary>
        ///     Gets the worker actor statistics from its internal state.
        /// </summary>
        /// <returns>The worker actor statistics.</returns>
        Task<Statistics> GetProcessingStatisticsAsync();
    }
}