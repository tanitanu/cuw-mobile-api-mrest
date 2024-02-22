using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPGaugeChange
    {
        public string Destination { get; set; } = string.Empty;
        public string DecodedDestination { get; set; } = string.Empty;
        public string Equipment { get; set; } = string.Empty;
        public string EquipmentDescription { get; set; } = string.Empty;

        //public string Destination
        //{
        //    get
        //    {
        //        return this.destination;
        //    }
        //    set
        //    {
        //        this.destination = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string DecodedDestination
        //{
        //    get
        //    {
        //        return this.decodedDestination;
        //    }
        //    set
        //    {
        //        this.decodedDestination = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string Equipment
        //{
        //    get
        //    {
        //        return this.equipment;
        //    }
        //    set
        //    {
        //        this.equipment = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string EquipmentDescription
        //{
        //    get
        //    {
        //        return this.equipmentDescription;
        //    }
        //    set
        //    {
        //        this.equipmentDescription = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}
    }
}
