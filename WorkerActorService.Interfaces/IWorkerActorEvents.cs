#region Copyright

//=======================================================================================
// Microsoft Azure Customer Advisory Team  
//
// This sample is supplemental to the technical guidance published on the community
// blog at http://blogs.msdn.com/b/paolos/. 
// 
// Author: Paolo Salvatori
//=======================================================================================
// Copyright © 2017 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
//=======================================================================================

#endregion

#region Using Directives

using Microsoft.ServiceFabric.Actors;

#endregion

namespace Microsoft.AzureCat.Samples.WorkerActorService.Interfaces
{
    /// <summary>
    /// Occurs when a message has been processed. 
    /// This event is raised when the processor returns a value.
    /// </summary>
    public interface IWorkerActorEvents : IActorEvents
    {
        void MessageProcessingCompleted(string messageId, long returnValue);
    }
}
