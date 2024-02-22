using System;

namespace United.Mobile.Model.Shopping
{
    [Serializable()]
    public class SHOPEquipmentDisclosure
    {
        public string AircraftDoorWidth { get; set; } = string.Empty;

        public string EquipmentDescription { get; set; } = string.Empty;

        public string EquipmentType { get; set; } = string.Empty;

        public bool IsSingleCabin { get; set; }
        public bool NoBoardingAssistance { get; set; }

        public bool NonJetEquipment { get; set; }

        public bool WheelchairsNotAllowed { get; set; }
        public string AircraftDoorHeight { get; set; } = string.Empty;
    }
}
