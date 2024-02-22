using System.Collections.Generic;

namespace United.Mobile.Model.CSLModels
{
    public class RewardProgramAwsResponse
    {
        public List<MOBCustomerDataAwsResponseError> Errors { get; set; }
        public int Status { get; set; }
        public string ServerName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }
}
