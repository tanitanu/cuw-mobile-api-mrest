using System;
using System.Collections.Generic;

namespace United.Mobile.Model.SeatMapEngine
{
    [Serializable()]
    public class MOBCabin
    {
        public MOBCabin() { }
        public MOBCabin(List<MOBRow> rows, string cos)
        {
            Rows = rows;
            COS = cos;
        }

        private List<MOBRow> rows = new List<MOBRow>();
        public List<MOBRow> Rows
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

        private string configuration;
        public string Configuration
        {
            get { return configuration; }
            set { configuration = value; }
        }

        public List<MOBRow> FrontMonuments { get; set; }
        public List<MOBRow> BackMonuments { get; set; }
    }
}
