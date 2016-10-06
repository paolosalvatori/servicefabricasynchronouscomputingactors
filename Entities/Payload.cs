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

using Newtonsoft.Json;

#endregion

namespace Microsoft.AzureCat.Samples.Entities
{
    /// <summary>
    ///     Represents messages sent to the gateway service.
    /// </summary>
    public class Payload
    {
        /// <summary>
        ///     Gets or sets the worker actor id.
        /// </summary>
        [JsonProperty(PropertyName = "workerId", Order = 1)]
        public string WorkerId { get; set; }

        /// <summary>
        ///     Gets or sets the message sent to the worker actor.
        /// </summary>
        [JsonProperty(PropertyName = "message", Order = 2)]
        public Message Message { get; set; }
    }
}