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
    ///     Represents the worker actor statistics.
    /// </summary>
    public class Statistics
    {
        /// <summary>
        ///     Gets or sets the number of received messages.
        /// </summary>
        [JsonProperty(PropertyName = "received", Order = 1)]
        public long Received { get; set; }

        /// <summary>
        ///     Gets or sets the number of complete messages.
        /// </summary>
        [JsonProperty(PropertyName = "complete", Order = 2)]
        public long Complete { get; set; }

        /// <summary>
        ///     Gets or sets the number of stopped messages.
        /// </summary>
        [JsonProperty(PropertyName = "stopped", Order = 3)]
        public long Stopped { get; set; }

        /// <summary>
        ///     Gets or sets the min value.
        /// </summary>
        [JsonProperty(PropertyName = "min", Order = 4)]
        public long MinValue { get; set; }

        /// <summary>
        ///     Gets or sets the max value.
        /// </summary>
        [JsonProperty(PropertyName = "max", Order = 5)]
        public long MaxValue { get; set; }

        /// <summary>
        ///     Gets or sets the total value.
        /// </summary>
        [JsonProperty(PropertyName = "tot", Order = 6)]
        public long TotalValue { get; set; }

        /// <summary>
        ///     Gets or sets the average value.
        /// </summary>
        [JsonProperty(PropertyName = "avg", Order = 7)]
        public double AverageValue { get; set; }

        /// <summary>
        ///     Gets or sets the average value.
        /// </summary>
        [JsonProperty(PropertyName = "results", Order = 8)]
        public IEnumerable<Result> Results { get; set; }
    }
}