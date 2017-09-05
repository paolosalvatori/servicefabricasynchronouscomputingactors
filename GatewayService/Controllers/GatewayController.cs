// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directices
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using System.Diagnostics;
using Microsoft.AzureCat.Samples.Entities;
using Microsoft.AzureCat.Samples.Framework;
using Microsoft.AzureCat.Samples.WorkerActorService.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
#endregion

namespace Microsoft.AzureCat.Samples.GatewayService
{
    public class GatewayController : ApiController
    {
        #region Private Static Fields

        private static readonly Dictionary<string, IWorkerActor> actorProxyDictionary =
            new Dictionary<string, IWorkerActor>();

        #endregion

        #region Private Static Methods

        private static IWorkerActor GetActorProxy(string workerId)
        {
            lock (actorProxyDictionary)
            {
                if (actorProxyDictionary.ContainsKey(workerId))
                    return actorProxyDictionary[workerId];
                actorProxyDictionary[workerId] = ActorProxy.Create<IWorkerActor>(
                    new ActorId(workerId),
                    new Uri(OwinCommunicationListener.WorkerActorServiceUri));
                return actorProxyDictionary[workerId];
            }
        }

        #endregion

        #region Private Constants

        //************************************
        // Parameters
        //************************************

        #endregion

        #region Public Methods

        [HttpGet]
        public string Test()
        {
            return "TEST";
        }

        /// <summary>
        ///     Starts processing a message in sequential order.
        /// </summary>
        /// <param name="payload">The payload containing the worker id and message to process.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        [HttpPost]
        [Route("api/sequential/start")]
        public async Task<bool> StartSequentialProcessingAsync(Payload payload)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(payload?.WorkerId) ||
                    string.IsNullOrWhiteSpace(payload.Message?.MessageId) ||
                    string.IsNullOrWhiteSpace(payload.Message.Body))
                    throw new ArgumentException($"Parameter {nameof(payload)} is null or invalid.", nameof(payload));

                // Gets actor proxy
                var proxy = GetActorProxy(payload.WorkerId);
                if (proxy == null)
                    return false;

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Calling WorkerActor[{payload.WorkerId}].StartSequentialProcessingAsync on MessageId=[{payload.Message.MessageId}]...");

                var isSuccess = true;
                var callwatch = new Stopwatch();

                try
                {
                    callwatch.Start();
                    return await proxy.StartSequentialProcessingAsync(payload.Message);
                }
                catch (Exception)
                {
                    // Sets success flag to false
                    isSuccess = false;
                    throw;
                }
                finally
                {
                    callwatch.Stop();
                    ServiceEventSource.Current.Dependency("WorkerActor",
                                                          isSuccess,
                                                          callwatch.ElapsedMilliseconds,
                                                          isSuccess ? "Succeded" : "Failed",
                                                          "Actor");
                }
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                {
                    return false;
                }
                foreach (var exception in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(exception);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
            return false;
        }

        /// <summary>
        ///     Starts processing a message on a separate task.
        /// </summary>
        /// <param name="payload">The payload containing the worker id and message to process.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        [HttpPost]
        [Route("api/parallel/start")]
        public async Task<bool> StartParallelProcessingAsync(Payload payload)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(payload?.WorkerId) ||
                    string.IsNullOrWhiteSpace(payload.Message?.MessageId) ||
                    string.IsNullOrWhiteSpace(payload.Message.Body))
                    throw new ArgumentException($"Parameter {nameof(payload)} is null or invalid.", nameof(payload));

                // Gets actor proxy
                var proxy = GetActorProxy(payload.WorkerId);
                if (proxy == null)
                    return false;

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Calling WorkerActor[{payload.WorkerId}].StartParallelProcessingAsync on MessageId=[{payload.Message.MessageId}]...");

                var isSuccess = true;
                var callwatch = new Stopwatch();

                try
                {
                    callwatch.Start();
                    return await proxy.StartParallelProcessingAsync(payload.Message);
                }
                catch (Exception)
                {
                    // Sets success flag to false
                    isSuccess = false;
                    throw;
                }
                finally
                {
                    callwatch.Stop();
                    ServiceEventSource.Current.Dependency("WorkerActor",
                                                          isSuccess,
                                                          callwatch.ElapsedMilliseconds,
                                                          isSuccess ? "Succeded" : "Failed",
                                                          "Actor");
                }
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                {
                    return false;
                }
                foreach (var exception in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(exception);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
            return false;
        }

        /// <summary>
        ///     Stops the sequential processing task of running in a specific worker actor.
        /// </summary>
        /// <param name="payload">The payload containing the worker id.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        [HttpPost]
        [Route("api/sequential/stop")]
        public async Task<bool> StopSequentialProcessingAsync(Payload payload)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(payload?.WorkerId))
                    throw new ArgumentException($"Parameter {nameof(payload)} is null or invalid.", nameof(payload));

                // Gets actor proxy
                var proxy = GetActorProxy(payload.WorkerId);
                if (proxy == null)
                    return false;

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Calling WorkerActor[{payload.WorkerId}].StopSequentialProcessingAsync...");

                var isSuccess = true;
                var callwatch = new Stopwatch();

                try
                {
                    callwatch.Start();
                    return await proxy.StopSequentialProcessingAsync();
                }
                catch (Exception)
                {
                    // Sets success flag to false
                    isSuccess = false;
                    throw;
                }
                finally
                {
                    callwatch.Stop();
                    ServiceEventSource.Current.Dependency("WorkerActor",
                                                          isSuccess,
                                                          callwatch.ElapsedMilliseconds,
                                                          isSuccess ? "Succeded" : "Failed",
                                                          "Actor");
                }
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                {
                    return false;
                }
                foreach (var exception in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(exception);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
            return false;
        }

        /// <summary>
        ///     Stops the elaboration of a specific message by a given worker actor.
        /// </summary>
        /// <param name="payload">The payload containing the worker id.</param>
        /// <returns>True if the operation completes successfully, false otherwise.</returns>
        [HttpPost]
        [Route("api/parallel/stop")]
        public async Task<bool> StopParallelProcessingAsync(Payload payload)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(payload?.WorkerId) ||
                    string.IsNullOrWhiteSpace(payload.Message?.MessageId))
                    throw new ArgumentException($"Parameter {nameof(payload)} is null or invalid.", nameof(payload));

                // Gets actor proxy
                var proxy = GetActorProxy(payload.WorkerId);
                if (proxy == null)
                    return false;

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Calling WorkerActor[{payload.WorkerId}].StopParallelProcessingAsync on MessageId=[{payload.Message.MessageId}]...");

                var isSuccess = true;
                var callwatch = new Stopwatch();

                try
                {
                    callwatch.Start();
                    return await proxy.StopParallelProcessingAsync(payload.Message.MessageId);
                }
                catch (Exception)
                {
                    // Sets success flag to false
                    isSuccess = false;
                    throw;
                }
                finally
                {
                    callwatch.Stop();
                    ServiceEventSource.Current.Dependency("WorkerActor",
                                                          isSuccess,
                                                          callwatch.ElapsedMilliseconds,
                                                          isSuccess ? "Succeded" : "Failed",
                                                          "Actor");
                }
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                {
                    return false;
                }
                foreach (var exception in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(exception);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
            return false;
        }

        /// <summary>
        ///     Checks if the sequential processing task running is a given worker actor is still active.
        /// </summary>
        /// <param name="payload">The payload containing the worker id.</param>
        /// <returns>True if sequential processing task is still running, false otherwise.</returns>
        [HttpPost]
        [Route("api/sequential/monitor")]
        public async Task<bool> IsSequentialProcessingRunningAsync(Payload payload)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(payload?.WorkerId))
                    throw new ArgumentException($"Parameter {nameof(payload)} is null or invalid.", nameof(payload));

                // Gets actor proxy
                var proxy = GetActorProxy(payload.WorkerId);
                if (proxy == null)
                    return false;

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Calling WorkerActor[{payload.WorkerId}].IsSequentialProcessingRunningAsync...");

                var isSuccess = true;
                var callwatch = new Stopwatch();

                try
                {
                    callwatch.Start();
                    return await proxy.IsSequentialProcessingRunningAsync();
                }
                catch (Exception)
                {
                    // Sets success flag to false
                    isSuccess = false;
                    throw;
                }
                finally
                {
                    callwatch.Stop();
                    ServiceEventSource.Current.Dependency("WorkerActor",
                                                          isSuccess,
                                                          callwatch.ElapsedMilliseconds,
                                                          isSuccess ? "Succeded" : "Failed",
                                                          "Actor");
                }
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                {
                    return false;
                }
                foreach (var exception in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(exception);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
            return false;
        }

        /// <summary>
        ///     Checks if the elaboration of a given message by a given worker actor is still active.
        /// </summary>
        /// <param name="payload">The payload containing the worker id.</param>
        /// <returns>True if sequential processing task is still running, false otherwise.</returns>
        [HttpPost]
        [Route("api/parallel/monitor")]
        public async Task<bool> IsParallelProcessingRunningAsync(Payload payload)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(payload?.WorkerId) ||
                    string.IsNullOrWhiteSpace(payload.Message?.MessageId))
                    throw new ArgumentException($"Parameter {nameof(payload)} is null or invalid.", nameof(payload));

                // Gets actor proxy
                var proxy = GetActorProxy(payload.WorkerId);
                if (proxy == null)
                    return false;

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Calling WorkerActor[{payload.WorkerId}].IsParallelProcessingRunningAsync on MessageId=[{payload.Message.MessageId}]...");

                var isSuccess = true;
                var callwatch = new Stopwatch();

                try
                {
                    callwatch.Start();
                    return await proxy.IsParallelProcessingRunningAsync(payload.Message.MessageId);
                }
                catch (Exception)
                {
                    // Sets success flag to false
                    isSuccess = false;
                    throw;
                }
                finally
                {
                    callwatch.Stop();
                    ServiceEventSource.Current.Dependency("WorkerActor",
                                                          isSuccess,
                                                          callwatch.ElapsedMilliseconds,
                                                          isSuccess ? "Succeded" : "Failed",
                                                          "Actor");
                }
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                {
                    return false;
                }
                foreach (var exception in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(exception);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
            return false;
        }

        /// <summary>
        ///     Gets the worker actor statistics from its internal state.
        ///     <param name="payload">The payload containing the worker id.</param>
        /// </summary>
        /// <returns>The worker actor statistics.</returns>
        [HttpPost]
        [Route("api/statistics")]
        public async Task<Statistics> GetProcessingStatisticsAsync(Payload payload)
        {
            try
            {
                // Validates parameter
                if (string.IsNullOrWhiteSpace(payload?.WorkerId))
                    throw new ArgumentException($"Parameter {nameof(payload)} is null or invalid.", nameof(payload));

                // Gets actor proxy
                var proxy = GetActorProxy(payload.WorkerId);
                if (proxy == null)
                    return null;

                // Invokes actor using proxy
                ServiceEventSource.Current.Message($"Calling WorkerActor[{payload.WorkerId}].GetProcessingStatisticsAsync...");

                var isSuccess = true;
                var callwatch = new Stopwatch();

                try
                {
                    callwatch.Start();
                    return await proxy.GetProcessingStatisticsAsync();
                }
                catch (Exception)
                {
                    // Sets success flag to false
                    isSuccess = false;
                    throw;
                }
                finally
                {
                    callwatch.Stop();
                    ServiceEventSource.Current.Dependency("WorkerActor",
                                                          isSuccess,
                                                          callwatch.ElapsedMilliseconds,
                                                          isSuccess ? "Succeded" : "Failed",
                                                          "Actor");
                }
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerExceptions?.Count > 0))
                {
                    return null;
                }
                foreach (var exception in ex.InnerExceptions)
                {
                    ServiceEventSource.Current.Error(exception);
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.Error(ex);
            }
            return null;
        }

        #endregion
    }
}