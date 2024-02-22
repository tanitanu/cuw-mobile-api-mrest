using System;
using System.Collections.Generic;

namespace United.Mobile.Model.CSLModels
{
    public class InsertRewardProgramAwsRequest
    {
        public List<RewardProgramAws> RewardPrograms { get; set; }
        
        public string InsertId  { get; set; }
    }
}
