using System;
using System.Collections.Generic;

namespace United.Mobile.Model.CSLModels
{
    public class UpdateRewardProgramAwsRequest
    {

        public List<RewardProgramAws> RewardPrograms { get; set; }
        public string UpdateId { get; set; }
    }

    public class RewardProgramAws
    {

        public int ProgramId { get; set; }
        public string ProgramMemberId { get; set; }
    }
}
