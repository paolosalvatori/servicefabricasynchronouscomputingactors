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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AzureCat.Samples.Entities;
using Microsoft.AzureCat.Samples.Framework.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

#endregion

namespace Microsoft.AzureCat.Samples.Framework
{
    public abstract class CircularQueueActor : Actor, ICircularQueueActor
    {
        #region Public Constructor

        /// <summary>
        ///     Initializes a new instance of CircularQueueActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public CircularQueueActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        #endregion

        #region Actor Overridden Methods

        protected override async Task OnActivateAsync()
        {
            // First Activation
            await StateManager.TryAddStateAsync(HeaderIndexState, (long) -1);
            await StateManager.TryAddStateAsync(TailIndexState, (long) -1);
        }

        #endregion

        #region Private Constants

        private const string TailIndexState = "tail";
        private const string HeaderIndexState = "header";

        #endregion

        #region Public Methods

        /// <summary>
        ///     Adds an object to the end of the circular queue.
        /// </summary>
        /// <param name="item">The object to add to the circular queue. The value cannot be null.</param>
        /// <returns>The asynchronous result of the operation.</returns>
        public virtual async Task EnqueueAsync(Message item)
        {
            try
            {
                if (item == null)
                {
                    throw new ArgumentException($"{nameof(item)} parameter cannot be null", nameof(item));
                }
                var result = await StateManager.TryGetStateAsync<long>(TailIndexState);
                if (!result.HasValue)
                {
                    return;
                }
                var tail = result.Value == long.MaxValue ? 0 : result.Value + 1;
                await StateManager.SetStateAsync(TailIndexState, tail);
                var key = tail.ToString();
                await StateManager.TryAddStateAsync(key, item);
                ActorEventSource.Current.Message($"Message successfully enqueued. Tail=[{tail}]"); 
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
        }

        /// <summary>
        ///     Removes and returns the object at the beginning of the circular queue.
        /// </summary>
        /// <returns>The object that is removed from the beginning of the circular queue.</returns>
        public virtual async Task<Message> DequeueAsync()
        {
            try
            {
                var headerResult = await StateManager.TryGetStateAsync<long>(HeaderIndexState);
                var tailResult = await StateManager.TryGetStateAsync<long>(TailIndexState);
                if (!headerResult.HasValue ||
                    !tailResult.HasValue ||
                    (headerResult.Value == tailResult.Value))
                {
                    return null;
                }
                var tail = tailResult.Value;
                var header = headerResult.Value == long.MaxValue ? 0 : headerResult.Value + 1;
                var key = header.ToString();
                var result = await StateManager.TryGetStateAsync<Message>(key);
                if (!result.HasValue)
                {
                    return null;
                }
                await StateManager.TryRemoveStateAsync(key);
                await StateManager.SetStateAsync(HeaderIndexState, header);
                ActorEventSource.Current.Message($"Message successfully dequeued. Header=[{header}] Tail=[{tail}]");
                return result.Value;
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
        }

        /// <summary>
        ///     Removes and returns a collection collection containing all the objects in the circular queue.
        /// </summary>
        /// <returns>A collection containing all the objects in the queue.</returns>
        public virtual async Task<IEnumerable<Message>> DequeueAllAsync()
        {
            try
            {
                var list = new List<Message>();
                var names = await StateManager.GetStateNamesAsync();
                var i = 0;
                foreach (var name in names)
                {
                    var result = await StateManager.TryGetStateAsync<Message>(name);
                    if (result.HasValue)
                        list.Add(result.Value);
                    await StateManager.TryRemoveStateAsync(name);
                    i++;
                }
                await StateManager.AddOrUpdateStateAsync(TailIndexState, 0, (k, v) => 0);
                await StateManager.AddOrUpdateStateAsync(HeaderIndexState, 0, (k, v) => 0);
                ActorEventSource.Current.Message($"[{i}] messages retrieved from the queue.");
                return list;
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
        }

        /// <summary>
        ///     Returns the object at the beginning of the circular queue without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the circular queue.</returns>
        public virtual async Task<Message> PeekAsync()
        {
            try
            {
                var header = await StateManager.GetStateAsync<long>(HeaderIndexState);
                var tail = await StateManager.GetStateAsync<long>(TailIndexState);
                if (tail == header)
                {
                    return null;
                }
                var current = header == long.MaxValue ? 0 : header + 1;
                var key = current.ToString();
                var result = await StateManager.TryGetStateAsync<Message>(key);
                ActorEventSource.Current.Message($"Message successfully peeked. Header=[{header}] Tail=[{tail}]");
                return !result.HasValue ? null : result.Value;
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
        }

        /// <summary>
        ///     Gets the count of the items contained in the circular queue.
        /// </summary>
        public virtual Task<long> Count
        {
            get
            {
                try
                {
                    var names = StateManager.GetStateNamesAsync().Result;
                    var enumerable = names as string[] ?? names.ToArray();
                    var i = enumerable.Any() ? enumerable.Length - 2: 0;
                    ActorEventSource.Current.Message($"There are [{i}] messages in the queue.");
                    return Task.FromResult<long>(i);
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.Error(ex);
                    throw;
                }
            }
        }

        #endregion
    }
}