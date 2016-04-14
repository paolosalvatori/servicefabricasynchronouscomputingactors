// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives



#endregion

namespace Microsoft.AzureCat.Samples.Entities
{
    using Newtonsoft.Json;

    /// <summary>
    /// Represents messages sent to the gateway service.
    /// </summary>
    public class Payload
    {
        /// <summary>
        /// Gets or sets the worker actor id.
        /// </summary>
        [JsonProperty(PropertyName = "workerId", Order = 1)]
        public string WorkerId { get; set; }

        /// <summary>
        /// Gets or sets the message sent to the worker actor.
        /// </summary>
        [JsonProperty(PropertyName = "message", Order = 2)]
        public Message Message { get; set; }
    }
}