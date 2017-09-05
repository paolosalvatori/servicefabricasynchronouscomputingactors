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

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Microsoft.AzureCat.Samples.Framework;

#endregion

namespace Microsoft.AzureCat.Samples.GatewayService
{
    public class EventSourceFilter : ActionFilterAttribute
    {
        #region Private Constants

        private const string StopwatchKey = "StopwatchFilter.Value";
        private const string Prefix = "Gateway";

        #endregion

        #region ActionFilterAttribute Overridden Methods

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);
            actionContext.Request.Properties[StopwatchKey] = Stopwatch.StartNew();
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            base.OnActionExecuted(actionExecutedContext);
            var stopwatch = (Stopwatch) actionExecutedContext.Request.Properties[StopwatchKey];
            stopwatch.Stop();
            ServiceEventSource.Current.RequestComplete(GetRequestName(actionExecutedContext.Request.RequestUri),
                actionExecutedContext.Response.IsSuccessStatusCode,
                stopwatch.ElapsedMilliseconds,
                $"{actionExecutedContext.Response.StatusCode}");
        }

        #endregion

        #region Private Static Methods

        private static string GetRequestName(Uri uri)
        {
            if (string.IsNullOrEmpty(uri?.AbsolutePath))
            {
                return "UNKNOWN";
            }
            var segmentList =
                uri.Segments.Select(s => s.Replace("/", "")).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            var index =
                segmentList.FindIndex(s => string.Compare(s, "api", StringComparison.InvariantCultureIgnoreCase) == 0);
            var stringBuilder = new StringBuilder(Prefix);
            for (var i = index + 1; i < segmentList.Count; i++)
            {
                stringBuilder.Append(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(segmentList[i]));
            }
            return stringBuilder.ToString();
        }

        #endregion
    }
}
