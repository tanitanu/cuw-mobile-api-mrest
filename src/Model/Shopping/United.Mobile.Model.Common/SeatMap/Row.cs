using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace United.Mobile.Model.Shopping
{
    [Serializable()]
    public class Row
    {
        public Row() { }

        public Row(List<SeatB> seats, string number, bool wing)
        {
            Number = number;
            Seats = seats;
            Wing = wing;
        }

        private List<SeatB> seats = new List<SeatB>();
        public List<SeatB> Seats
        {
            get { return seats; }
            set { seats = value; }
        }

        private string number;
        public string Number
        {
            get { return number; }
            set { number = value; }
        }

        private bool wing;
        public bool Wing
        {
            get { return wing; }
            set { wing = value; }
        }
    }
}
