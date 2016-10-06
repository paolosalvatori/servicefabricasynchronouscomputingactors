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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AzureCat.Samples.Entities;
using Microsoft.ServiceFabric.Actors;

#endregion

namespace Microsoft.AzureCat.Samples.Framework.Interfaces
{
    public interface ICircularQueueActor : IActor
    {
        /// <summary>
        ///     Gets the count of the items contained in the circular queue.
        /// </summary>
        Task<long> Count { get; }

        /// <summary>
        ///     Adds an object to the end of the circular queue.
        /// </summary>
        /// <param name="item">The object to add to the circular queue. The value cannot be null.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        Task EnqueueAsync(Message item);

        /// <summary>
        ///     Removes and returns the object at the beginning of the circular queue.
        /// </summary>
        /// <returns>The object that is removed from the beginning of the circular queue.</returns>
        Task<Message> DequeueAsync();

        /// <summary>
        ///     Removes and returns a collection collection containing all the objects in the circular queue.
        /// </summary>
        /// <returns>A collection containing all the objects in the queue.</returns>
        Task<IEnumerable<Message>> DequeueAllAsync();

        /// <summary>
        ///     Returns the object at the beginning of the circular queue without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the circular queue.</returns>
        Task<Message> PeekAsync();
    }
}