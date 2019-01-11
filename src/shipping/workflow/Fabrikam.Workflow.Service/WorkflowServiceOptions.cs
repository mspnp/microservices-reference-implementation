namespace Fabrikam.Workflow.Service
{
    internal class WorkflowServiceOptions
    {
        public WorkflowServiceOptions()
        {
            MaxConcurrency = 10;
        }

        public string QueueEndpoint { get; set; }

        public string QueueName { get; set; }

        public int MaxConcurrency { get; internal set; }
    }
}
