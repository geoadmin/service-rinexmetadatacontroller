using System.IO;
using System.Xml;

namespace RinexMetaDataController
{
    class XMLHelper
    {
        private LogWriter lwSingleton = LogWriter.GetInstance;
        private string xmlPath;
        private string xmlFile;
        private string commentary;

        public string Commentary
        {
            get
            {
                return commentary;
            }

            set
            {
                commentary = value;
            }
        }

        public XMLHelper(string xmlPath, string xmlFile)
        {
            this.xmlPath = xmlPath;
            this.xmlFile = xmlFile;
        }

        public void writeToXML(HeaderInfo headerInfo)
        {
            string serializationFile = Path.Combine(xmlPath, xmlFile);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(serializationFile, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("VectorObject");
                writer.WriteAttributeString("xmlns", "xsi", "", "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xsi","noNamespaceSchemaLocation", "","../../Models/RINEX/RINEX3.OBJ.XSD");
                writer.WriteElementString("GDSKey", "RINEX");
                writer.WriteElementString("LayerKey", "RINEX_STATIONS");
                writer.WriteElementString("TemporalKey", "2019");
                writer.WriteElementString("ReleaseKey", "1");
                writer.WriteElementString("Version", "1");
                writer.WriteElementString("Status", "valid");
                writer.WriteElementString("StatusDate", System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                writer.WriteElementString("ImportDate", "");
                writer.WriteElementString("SourceReferenceSystem", "CHLV95");
                writer.WriteElementString("Commentary", commentary);
                writer.WriteStartElement("Checksum");
                writer.WriteAttributeString("Algorithmus", "MD5");
                writer.WriteAttributeString("value",headerInfo.ChecksumMD5);
                writer.WriteEndElement();
                writer.WriteElementString("RinexVersion", headerInfo.RinexVersion);
                writer.WriteElementString("RinexShortName", headerInfo.RinexShortName);
                writer.WriteElementString("RinexLongName", headerInfo.RinexLongname);
                writer.WriteElementString("Name", headerInfo.Name);
                writer.WriteElementString("X", headerInfo.X.ToString());
                writer.WriteElementString("Y", headerInfo.Y.ToString());
                writer.WriteElementString("Z", headerInfo.Z.ToString());
                writer.WriteStartElement("Intervall");
                writer.WriteAttributeString("value", headerInfo.Intervall.ToString());
                writer.WriteAttributeString("unit", "second");
                writer.WriteEndElement();
                writer.WriteElementString("FirstObservation", headerInfo.FirstObservation);
                writer.WriteElementString("LastObservation", headerInfo.LastObservation);
                writer.WriteElementString("NumberOfObservations", headerInfo.NumberOfObservations.ToString());
                writer.WriteElementString("NumberOfSatellites", headerInfo.NumberOfSatellites.ToString());
                writer.WriteElementString("Observer", headerInfo.Observer);
                writer.WriteElementString("ObserverType", headerInfo.ObserverType);
                writer.WriteElementString("ObserverNr", headerInfo.ObserverNr);
                writer.WriteElementString("ObserverFirmware", headerInfo.ObserverFirmware);
                writer.WriteElementString("GnssTimeSystem", headerInfo.GnssTimeSystem);
                writer.WriteElementString("AntennaType", headerInfo.AntennaType);
                writer.WriteElementString("AntennaNr", headerInfo.AntennaNr);
                writer.WriteElementString("AntennaDeltaE", headerInfo.AntennaDeltaE.ToString());
                writer.WriteElementString("AntennaDeltaN", headerInfo.AntennaDeltaN.ToString());
                writer.WriteElementString("AntennaDeltaH", headerInfo.AntennaDeltaH.ToString());
                writer.WriteElementString("Agency", headerInfo.Agency);
                writer.WriteElementString("SatelliteSystem", headerInfo.SatelliteSystem);
                writer.WriteElementString("CreatorInfo", headerInfo.CreatorInfo);
                writer.WriteElementString("CreatorName", headerInfo.CreatorName);
                writer.WriteElementString("CreationDateTime", headerInfo.CreationDateTime);
                writer.WriteElementString("GpsObservationTypes", headerInfo.GpsObservationTypes.ToString());
                writer.WriteElementString("GloObservationTypes", headerInfo.GloObservationTypes.ToString());
                writer.WriteElementString("GalObservationTypes", headerInfo.GalObservationTypes.ToString());
                writer.WriteElementString("BdsObservationTypes", headerInfo.BdsObservationTypes.ToString());
                writer.WriteStartElement("Checksum");
                writer.WriteAttributeString("Algorithmus", "SHA256");
                writer.WriteAttributeString("value", headerInfo.Checksum);
                writer.WriteEndElement();
                writer.WriteElementString("IsShop", headerInfo.IsShop.ToString());
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }
    }
}
