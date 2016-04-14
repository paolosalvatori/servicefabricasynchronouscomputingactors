// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives



#endregion

namespace Microsoft.AzureCat.Samples.WorkerActorService
{
    using Microsoft.AzureCat.Samples.Framework;
    using Microsoft.AzureCat.Samples.WorkerActorService.Interfaces;
    using Microsoft.ServiceFabric.Actors.Runtime;

    /// <remarks>
    /// This actor can be used to start, stop and monitor long running processes.
    /// </remarks>
    [ActorService(Name = "QueueActorService")]
    [StatePersistence(StatePersistence.Persisted)]
    internal class QueueActor : CircularQueueActor, IQueueActor
    {
    }
}