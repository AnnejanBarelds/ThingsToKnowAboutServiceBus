namespace WebApp
{
    public class SessionState
    {
        public JobResult? Job1Result { get; set; }
        public JobResult? Job2Result { get; set; }

        public bool SetJobResult(JobResult jobResult)
        {
            switch (jobResult.JobId)
            {
                case 1:
                    Job1Result = jobResult;
                    break;
                case 2:
                    Job2Result = jobResult;
                    break;
                default:
                    throw new ArgumentException();
            }

            return Job1Result != null && Job2Result != null;
        }
    }
}
