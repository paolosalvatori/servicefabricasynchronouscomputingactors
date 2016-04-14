// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives



#endregion

namespace Microsoft.AzureCat.Samples.Entities
{
    using Newtonsoft.Json;

    public class Result
    {
        /// <summary>
        /// Gets or sets the message id.
        /// </summary>
        [JsonProperty(PropertyName = "messageId", Order = 1)]
        public string MessageId { get; set; }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        [JsonProperty(PropertyName = "returnValue", Order = 2)]
        public long ReturnValue { get; set; }
    }
}