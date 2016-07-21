// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives



#endregion

using Microsoft.AzureCat.Samples.Entities;

namespace Microsoft.AzureCat.Samples.WorkerActorService.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;

    /// <summary>
    /// This interface represents the actions a client app can perform on an actor.
    /// It MUST derive from IActor and all methods MUST return a Task.
    /// </summary>
    public interface IProcessorActor : IActor
    {
        /// <summary>
        /// Starts processing messages from the work queue in a sequential order.
        /// </summary>
        /// <param name="cancellationToken">This CancellationToken is used to stop message processing.</param>
        /// <returns></returns>
        Task ProcessSequentialMessagesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Starts processing messages from the work queue in a parallel order.
        /// </summary>
        /// <param name="workerId">The actor id as string of thr worker actor that invoked the processor actor</param>
        /// <param name="message">The message to process</param>
        /// <param name="cancellationToken">This CancellationToken is used to stop message processing.</param>
        /// <returns></returns>
        Task ProcessParallelMessagesAsync(string workerId, Message message, CancellationToken cancellationToken);
    }
}