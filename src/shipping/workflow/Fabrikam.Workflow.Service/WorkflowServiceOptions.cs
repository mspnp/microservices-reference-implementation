// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.Workflow.Service
{
    internal class WorkflowServiceOptions
    {
        public WorkflowServiceOptions()
        {
            MaxConcurrency = 20;
            PrefetchCount = 3000;
        }

        public string QueueEndpoint { get; set; }

        public string QueueName { get; set; }

        public string QueueAccessPolicyName { get; set; }

        public string QueueAccessPolicyKey { get; set; }

        public int MaxConcurrency { get; internal set; }

        public int PrefetchCount { get; internal set; }
    }
}
