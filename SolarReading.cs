using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SajSolarToMqtt
{
    [XmlRoot("real_time_data")]
    [XmlType("real_time_data")]
    public class SolarReading
    {
        [XmlElement(ElementName = "e-today")]
        public double EnergyToday { get; set; }

        [XmlElement(ElementName = "e-total")]
        public double EnergyTotal { get; set; }


        [XmlElement(ElementName = "p-ac")]
        public double Power { get; set; }

        
    }
}
