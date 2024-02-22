using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using United.Mobile.Model.SeatMapEngine;

namespace United.Mobile.Model.Shopping
{
    [Serializable()]
    public class Cabin
    {
        public Cabin() { }
        public Cabin(List<Row> rows, string cos)
        {
            Rows = rows;
            COS = cos;
        }

        private List<Row> rows = new List<Row>();
        public List<Row> Rows
        {
            get { return rows; }
            set { rows = value; }
        }

        private string cos = string.Empty;
        public string COS
        {
            get { return cos; }
            set { cos = value; }
        }
        public bool HasAvailableSeats { get; set; }
        public bool HasEnoughPcuSeats { get; set; }
        private string configuration;
        public string Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }
        public List<Row> FrontMonuments { get; set; }
        public List<Row> BackMonuments { get; set; }

    }
}
