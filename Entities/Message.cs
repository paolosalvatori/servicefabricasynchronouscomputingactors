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

using System.Collections.Generic;
using Newtonsoft.Json;

#endregion

namespace Microsoft.AzureCat.Samples.Entities
{
    /// <summary>
    ///     Represents messages elaborated by the worker and processor actors.
    /// </summary>
    public class Message
    {
        /// <summary>
        ///     Gets or sets the message id.
        /// </summary>
        [JsonProperty(PropertyName = "messageId", Order = 1)]
        public string MessageId { get; set; }

        /// <summary>
        ///     Gets or sets the message body.
        /// </summary>
        [JsonProperty(PropertyName = "body", Order = 2)]
        public string Body { get; set; }

        /// <summary>
        ///     Gets or sets the message properties
        /// </summary>
        [JsonProperty(PropertyName = "properties", Order = 3)]
        public IDictionary<string, object> Properties { get; set; }
    }
}