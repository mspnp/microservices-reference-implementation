// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.LoadTesting;

namespace Fabrikam.Shipping.LoadTests
{
    public class ContextParameterLoadTestPlugin : ILoadTestPlugin
    {
        public void Initialize(LoadTest loadTest)
        {
            loadTest.TestStarting += (s, e) =>
            {
                foreach (string key in loadTest.Context.Keys)
                {
                    e.TestContextProperties.Add(key, loadTest.Context[key]);
                }
            };
        }
    }
}
