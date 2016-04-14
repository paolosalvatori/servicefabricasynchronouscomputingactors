// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives

#endregion

namespace Microsoft.AzureCat.Samples.Entities
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents messages elaborated by the worker and processor actors.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the message id.
        /// </summary>
        [JsonProperty(PropertyName = "messageId", Order = 1)]
        public string MessageId { get; set; }

        /// <summary>
        /// Gets or sets the message body.
        /// </summary>
        [JsonProperty(PropertyName = "body", Order = 2)]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the message properties
        /// </summary>
        [JsonProperty(PropertyName = "properties", Order = 3)]
        public IDictionary<string, object> Properties { get; set; }
    }
}