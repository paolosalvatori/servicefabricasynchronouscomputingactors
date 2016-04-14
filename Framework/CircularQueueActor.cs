// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives



#endregion

namespace Microsoft.AzureCat.Samples.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.Entities;
    using Microsoft.AzureCat.Samples.Framework.Interfaces;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;

    public abstract class CircularQueueActor : Actor, ICircularQueueActor
    {
        #region Actor Overridden Methods

        protected override async Task OnActivateAsync()
        {
            // First Activation
            await this.StateManager.TryAddStateAsync(HeaderIndexState, (long) -1);
            await this.StateManager.TryAddStateAsync(TailIndexState, (long) -1);
        }

        #endregion

        #region Private Constants

        private const string TailIndexState = "tail";
        private const string HeaderIndexState = "header";

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an object to the end of the circular queue.
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
                ConditionalValue<long> result = await this.StateManager.TryGetStateAsync<long>(TailIndexState);
                if (!result.HasValue)
                {
                    return;
                }
                long tail = result.Value == long.MaxValue ? 0 : result.Value + 1;
                await this.StateManager.SetStateAsync(TailIndexState, tail);
                string key = tail.ToString();
                await this.StateManager.TryAddStateAsync(key, item);
                ActorEventSource.Current.Message($"Message successfully enqueued. Tail=[{tail}]");
            }
            catch (Exception ex)
            {
                ActorEventSource.Current.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the circular queue.
        /// </summary>
        /// <returns>The object that is removed from the beginning of the circular queue.</returns>
        public virtual async Task<Message> DequeueAsync()
        {
            try
            {
                ConditionalValue<long> headerResult = await this.StateManager.TryGetStateAsync<long>(HeaderIndexState);
                ConditionalValue<long> tailResult = await this.StateManager.TryGetStateAsync<long>(TailIndexState);
                if (!headerResult.HasValue ||
                    !tailResult.HasValue ||
                    headerResult.Value == tailResult.Value)
                {
                    return null;
                }
                long tail = tailResult.Value;
                long header = headerResult.Value == long.MaxValue ? 0 : headerResult.Value + 1;
                string key = header.ToString();
                ConditionalValue<Message> result = await this.StateManager.TryGetStateAsync<Message>(key);
                if (!result.HasValue)
                {
                    return null;
                }
                await this.StateManager.TryRemoveStateAsync(key);
                await this.StateManager.SetStateAsync(HeaderIndexState, header);
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
        /// Removes and returns a collection collection containing all the objects in the circular queue.
        /// </summary>
        /// <returns>A collection containing all the objects in the queue.</returns>
        public virtual async Task<IEnumerable<Message>> DequeueAllAsync()
        {
            try
            {
                List<Message> list = new List<Message>();
                IEnumerable<string> names = await this.StateManager.GetStateNamesAsync();
                int i = 0;
                foreach (string name in names)
                {
                    ConditionalValue<Message> result = await this.StateManager.TryGetStateAsync<Message>(name);
                    if (result.HasValue)
                    {
                        list.Add(result.Value);
                    }
                    await this.StateManager.TryRemoveStateAsync(name);
                    i++;
                }
                await this.StateManager.AddOrUpdateStateAsync(TailIndexState, 0, (k, v) => 0);
                await this.StateManager.AddOrUpdateStateAsync(HeaderIndexState, 0, (k, v) => 0);
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
        /// Returns the object at the beginning of the circular queue without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the circular queue.</returns>
        public virtual async Task<Message> PeekAsync()
        {
            try
            {
                long header = await this.StateManager.GetStateAsync<long>(HeaderIndexState);
                long tail = await this.StateManager.GetStateAsync<long>(TailIndexState);
                if (tail == header)
                {
                    return null;
                }
                long current = header == long.MaxValue ? 0 : header + 1;
                string key = current.ToString();
                ConditionalValue<Message> result = await this.StateManager.TryGetStateAsync<Message>(key);
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
        /// Gets the count of the items contained in the circular queue.
        /// </summary>
        public virtual Task<long> Count
        {
            get
            {
                try
                {
                    IEnumerable<string> names = this.StateManager.GetStateNamesAsync().Result;
                    string[] enumerable = names as string[] ?? names.ToArray();
                    int i = enumerable.Any() ? enumerable.Count() : 0;
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