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

using Microsoft.AzureCat.Samples.Framework;
using Microsoft.AzureCat.Samples.WorkerActorService.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

#endregion

namespace Microsoft.AzureCat.Samples.WorkerActorService
{
    /// <remarks>
    ///     This actor can be used to start, stop and monitor long running processes.
    /// </remarks>
    [ActorService(Name = "QueueActorService")]
    [StatePersistence(StatePersistence.Persisted)]
    internal class QueueActor : CircularQueueActor, IQueueActor
    {
        #region Public Constructor

        /// <summary>
        ///     Initializes a new instance of QueueActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public QueueActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        #endregion
    }
}