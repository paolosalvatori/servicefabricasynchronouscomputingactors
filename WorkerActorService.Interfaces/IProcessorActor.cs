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

using System.Threading;
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
    public interface IProcessorActor : IActor
    {
        /// <summary>
        ///     Starts processing messages from the work queue in a sequential order.
        /// </summary>
        /// <param name="cancellationToken">This CancellationToken is used to stop message processing.</param>
        /// <returns></returns>
        Task ProcessSequentialMessagesAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Starts processing messages from the work queue in a parallel order.
        /// </summary>
        /// <param name="workerId">The actor id as string of thr worker actor that invoked the processor actor</param>
        /// <param name="message">The message to process</param>
        /// <param name="cancellationToken">This CancellationToken is used to stop message processing.</param>
        /// <returns></returns>
        Task ProcessParallelMessagesAsync(string workerId, Message message, CancellationToken cancellationToken);
    }
}