using System;

namespace RinexMetaDataController
{

    [Serializable]
    class HeaderInfo
    {
        private string rinexVersion;
        private string rinexShortName;
        private string rinexLongName;
        private string name;
        private double x;
        private double y;
        private double z;
        private double intervall;
        private string firstObservation;
        private string lastObservation;
        private int numberOfObservations;
        private int numberOfSatellites;
        private string observer;
        private string observerType;
        private string observerNr;
        private string observerFirmware;
        private string gnssTimeSystem;
        private string antennaType;
        private string antennaNr;
        private double antennaDeltaE;
        private double antennaDeltaN;
        private double antennaDeltaH;
        private string agency;
        private string satelliteSystem;
        private string creatorInfo;
        private string creatorName;
        private string creationDateTime;
        private int gpsObservationTypes;
        private int gloObservationTypes;
        private int galObservationTypes;
        private int bdsObservationTypes;
        private string checksum;
        private string checksumMD5;
        private bool isShop;

        public HeaderInfo(string rinexVersion, string rinexShortName, string rinexLongName, string name, double x, double y, double z, double intervall, string firstObservation, string lastObservation, int numberOfObservations, int numberOfSatellites,
            string observer, string observerType, string observerNr, string observerFirmware, string gnssTimeSystem, string antennaType, string antennaNr, double antennaDeltaE, double antennaDeltaN,
            double antennaDeltaH, string agency, string satelliteSystem, string creatorInfo, string creatorName, string creationDateTime, int gpsObservationTypes, int gloObservationTypes, int galObservationTypes, int bdsObservationTypes, string checksum, string checksumMD5, bool isShop)
        {
            this.rinexVersion = rinexVersion;
            this.rinexShortName = rinexShortName;
            this.rinexLongName = rinexLongName;
            this.name = name;
            this.x = x;
            this.y = y;
            this.z = z;
            this.intervall = intervall;
            this.firstObservation = firstObservation;
            this.lastObservation = lastObservation;
            this.numberOfObservations = numberOfObservations;
            this.numberOfSatellites = numberOfSatellites;
            this.observer = observer;
            this.observerType = observerType;
            this.observerNr = observerNr;
            this.observerFirmware = observerFirmware;
            this.gnssTimeSystem = gnssTimeSystem;
            this.antennaType = antennaType;
            this.antennaNr = antennaNr;
            this.antennaDeltaE = antennaDeltaE;
            this.antennaDeltaN = antennaDeltaN;
            this.antennaDeltaH = antennaDeltaH;
            this.agency = agency;
            this.satelliteSystem = satelliteSystem;
            this.creatorInfo = creatorInfo;
            this.creatorName = creatorName;
            this.creationDateTime = creationDateTime;
            this.gpsObservationTypes = gpsObservationTypes;
            this.gloObservationTypes = gloObservationTypes;
            this.galObservationTypes = galObservationTypes;
            this.bdsObservationTypes = bdsObservationTypes;
            this.checksum = checksum;
            this.checksumMD5 = checksumMD5;
            this.isShop = isShop;
        }
        public string RinexVersion
        {
            get
            {
                return rinexVersion;
            }

            set
            {
                rinexVersion = value;
            }
        }

        public string RinexShortName
        {
            get
            {
                return rinexShortName;
            }

            set
            {
                rinexShortName = value;
            }
        }

        public string RinexLongname
        {
            get
            {
                return rinexLongName;
            }

            set
            {
                rinexLongName = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public double X
        {
            get
            {
                return x;
            }

            set
            {
                x = value;
            }
        }

        public double Y
        {
            get
            {
                return y;
            }

            set
            {
                y = value;
            }
        }

        public double Z
        {
            get
            {
                return z;
            }

            set
            {
                z = value;
            }
        }

        public double Intervall
        {
            get
            {
                return intervall;
            }

            set
            {
                intervall = value;
            }
        }

        public string FirstObservation
        {
            get
            {
                return firstObservation;
            }

            set
            {
                firstObservation = value;
            }
        }

        public string LastObservation
        {
            get
            {
                return lastObservation;
            }

            set
            {
                lastObservation = value;
            }
        }

        public int NumberOfObservations
        {
            get
            {
                return numberOfObservations;
            }

            set
            {
                numberOfObservations = value;
            }
        }

        public string Observer
        {
            get
            {
                return observer;
            }

            set
            {
                observer = value;
            }
        }

        public string Checksum
        {
            get
            {
                return checksum;
            }

            set
            {
                checksum = value;
            }
        }


        public string ChecksumMD5
        {
            get
            {
                return checksumMD5;
            }

            set
            {
                checksumMD5 = value;
            }
        }

        public string ObserverNr
        {
            get
            {
                return observerNr;
            }

            set
            {
                observerNr = value;
            }
        }

        public string ObserverFirmware
        {
            get
            {
                return observerFirmware;
            }

            set
            {
                observerFirmware = value;
            }
        }

        public string AntennaType
        {
            get
            {
                return antennaType;
            }

            set
            {
                antennaType = value;
            }
        }

        public string AntennaNr
        {
            get
            {
                return antennaNr;
            }

            set
            {
                antennaNr = value;
            }
        }

        public double AntennaDeltaE
        {
            get
            {
                return antennaDeltaE;
            }

            set
            {
                antennaDeltaE = value;
            }
        }

        public double AntennaDeltaN
        {
            get
            {
                return antennaDeltaN;
            }

            set
            {
                antennaDeltaN = value;
            }
        }

        public double AntennaDeltaH
        {
            get
            {
                return antennaDeltaH;
            }

            set
            {
                antennaDeltaH = value;
            }
        }

        public string Agency
        {
            get
            {
                return agency;
            }

            set
            {
                agency = value;
            }
        }

        public string SatelliteSystem
        {
            get
            {
                return satelliteSystem;
            }

            set
            {
                satelliteSystem = value;
            }
        }

        public string CreatorInfo
        {
            get
            {
                return creatorInfo;
            }

            set
            {
                creatorInfo = value;
            }
        }

        public string CreatorName
        {
            get
            {
                return creatorName;
            }

            set
            {
                creatorName = value;
            }
        }

        public string CreationDateTime
        {
            get
            {
                return creationDateTime;
            }

            set
            {
                creationDateTime = value;
            }
        }

        public string GnssTimeSystem
        {
            get
            {
                return gnssTimeSystem;
            }

            set
            {
                gnssTimeSystem = value;
            }
        }

        public string ObserverType
        {
            get
            {
                return observerType;
            }

            set
            {
                observerType = value;
            }
        }

        public int NumberOfSatellites
        {
            get
            {
                return numberOfSatellites;
            }

            set
            {
                numberOfSatellites = value;
            }
        }

        public int GpsObservationTypes
        {
            get
            {
                return gpsObservationTypes;
            }

            set
            {
                gpsObservationTypes = value;
            }
        }

        public int GloObservationTypes
        {
            get
            {
                return gloObservationTypes;
            }

            set
            {
                gloObservationTypes = value;
            }
        }

        public int GalObservationTypes
        {
            get
            {
                return galObservationTypes;
            }

            set
            {
                galObservationTypes = value;
            }
        }

        public int BdsObservationTypes
        {
            get
            {
                return bdsObservationTypes;
            }

            set
            {
                bdsObservationTypes = value;
            }
        }
        public bool IsShop
        {
            get
            {
                return isShop;
            }

            set
            {
                isShop = value;
            }
        }
    }
}