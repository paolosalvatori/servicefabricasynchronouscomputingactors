// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives



#endregion

namespace Microsoft.AzureCat.Samples.Framework.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.Entities;
    using Microsoft.ServiceFabric.Actors;

    public interface ICircularQueueActor : IActor
    {
        /// <summary>
        /// Gets the count of the items contained in the circular queue.
        /// </summary>
        Task<long> Count { get; }

        /// <summary>
        /// Adds an object to the end of the circular queue.
        /// </summary>
        /// <param name="item">The object to add to the circular queue. The value cannot be null.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task EnqueueAsync(Message item);

        /// <summary>
        /// Removes and returns the object at the beginning of the circular queue.
        /// </summary>
        /// <returns>The object that is removed from the beginning of the circular queue.</returns>
        Task<Message> DequeueAsync();

        /// <summary>
        /// Removes and returns a collection collection containing all the objects in the circular queue.
        /// </summary>
        /// <returns>A collection containing all the objects in the queue.</returns>
        Task<IEnumerable<Message>> DequeueAllAsync();

        /// <summary>
        /// Returns the object at the beginning of the circular queue without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the circular queue.</returns>
        Task<Message> PeekAsync();
    }
}