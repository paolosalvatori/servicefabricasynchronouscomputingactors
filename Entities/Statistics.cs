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
    /// Represents the worker actor statistics.
    /// </summary>
    public class Statistics
    {
        /// <summary>
        /// Gets or sets the number of received messages.
        /// </summary>
        [JsonProperty(PropertyName = "received", Order = 1)]
        public long Received { get; set; }

        /// <summary>
        /// Gets or sets the number of complete messages.
        /// </summary>
        [JsonProperty(PropertyName = "complete", Order = 2)]
        public long Complete { get; set; }

        /// <summary>
        /// Gets or sets the number of stopped messages.
        /// </summary>
        [JsonProperty(PropertyName = "stopped", Order = 3)]
        public long Stopped { get; set; }

        /// <summary>
        /// Gets or sets the min value.
        /// </summary>
        [JsonProperty(PropertyName = "min", Order = 4)]
        public long MinValue { get; set; }

        /// <summary>
        /// Gets or sets the max value.
        /// </summary>
        [JsonProperty(PropertyName = "max", Order = 5)]
        public long MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the total value.
        /// </summary>
        [JsonProperty(PropertyName = "tot", Order = 6)]
        public long TotalValue { get; set; }

        /// <summary>
        /// Gets or sets the average value.
        /// </summary>
        [JsonProperty(PropertyName = "avg", Order = 7)]
        public double AverageValue { get; set; }

        /// <summary>
        /// Gets or sets the average value.
        /// </summary>
        [JsonProperty(PropertyName = "results", Order = 8)]
        public IEnumerable<Result> Results { get; set; }
    }
}