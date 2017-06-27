// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives

#endregion

using Owin;

namespace Microsoft.AzureCat.Samples.GatewayService
{
    public interface IOwinAppBuilder
    {
        void Configuration(IAppBuilder appBuilder);
    }
}