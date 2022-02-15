using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;

namespace RinexMetaDataController
{
    class DBHelper : IDisposable
    {

        private SQLiteConnection sql_con;
        private LogWriter lwSingleton = LogWriter.GetInstance;

        public DBHelper(string dbName)
        {
            string sqlTable = "";
            string sqlView = "";
            string sqlView1 = "";
            string sqlView2 = "";
            string dateSeparator = Thread.CurrentThread.CurrentCulture.DateTimeFormat.DateSeparator;

            if (!File.Exists(dbName))
            {
                sqlTable = "CREATE TABLE \"HeaderInfo\"( `RinexVersion` TEXT,`RinexShortName` TEXT,`RinexLongName` TEXT, `Name` TEXT, `X` REAL, `Y` REAL, `Z` REAL, `Intervall` NUMERIC, `FirstObservation` TEXT, `LastObservation` TEXT, `NumberOfObservations` NUMERIC, `NumberOfSatellites` INTEGER, `Observer` TEXT, `ObserverType` TEXT, `ObserverNr` TEXT, `ObserverFirmware` TEXT, `GnssTimeSystem` TEXT, `AntennaType` TEXT, `AntennaNr` TEXT, `AntennaDeltaE` NUMERIC, `AntennaDeltaN` NUMERIC, `AntennaDeltaH` NUMERIC, `Agency` TEXT, `SatelliteSystem` TEXT, `CreatorInfo` NUMERIC, `CreatorName` INTEGER, `CreationDateTime` TEXT, `GpsObservationTypes` INTEGER, `GloObservationTypes` INTEGER, `GalObservationTypes` INTEGER, `BdsObservationTypes` INTEGER, `Checksum` TEXT UNIQUE, `IsShop` BOOL)";
                sqlView = "CREATE VIEW \"TotalStatistik\" as select Name,substr(FirstObservation,0,11) as 'Erste Beobachtung',sum(NumberofObservations) as 'Anzahl Observations pro Tag',count(Name) as 'Anzahl Stundenbeobachtungen',sum(NumberofObservations) / count(Name) as 'Durchschnitt pro Stunde',sum(NumberofObservations) / 864 as 'Prozent erfüllt pro Tag' from HeaderInfo group by Name, substr(FirstObservation,0,11) order by sum(NumberofObservations) asc";
                sqlView1 = String.Format("CREATE VIEW \"TagesStatistik\" as select * from TotalStatistik a where a.'Erste Beobachtung' like strftime('%d{0}%m{0}%Y', date('now')) or a.'Erste Beobachtung' like strftime('%d{0}%m{0}%Y', date('now','-1 days')) or a.'Erste Beobachtung' like strftime('%d{0}%m{0}%Y', date('now', '-2 days')) or a.'Erste Beobachtung' like strftime('%d{0}%m{0}%Y', date('now', '-3 days')) order by a.'Erste Beobachtung', a.'Prozent erfüllt pro Tag', a.Name asc", dateSeparator);
                sqlView2 = "CREATE VIEW \"Statistiknicht100\" as select * from TotalStatistik a where a.'Prozent erfüllt pro Tag' < 100 order by a.'Prozent erfüllt pro Tag' asc";
            }
            this.sql_con = new SQLiteConnection(String.Format("Data Source={0}; Version=3;", dbName));
            sql_con.Open();
            SQLiteCommand commandTable = new SQLiteCommand(sqlTable, sql_con);
            commandTable.ExecuteNonQuery();
            SQLiteCommand commandView = new SQLiteCommand(sqlView, sql_con);
            commandView.ExecuteNonQuery();
            SQLiteCommand commandView1 = new SQLiteCommand(sqlView1, sql_con);
            commandView1.ExecuteNonQuery();
            SQLiteCommand commandView2 = new SQLiteCommand(sqlView2, sql_con);
            commandView2.ExecuteNonQuery();
        }

        public void loadDBList(List<HeaderInfo> rinex)
        {
            foreach (HeaderInfo r in rinex)
            {
                CreateInsert(r);
            }
        }

        public void loadDBRecord(HeaderInfo rinexRecord)
        {
            if (!ExistingRecords(rinexRecord))
            {
                CreateInsert(rinexRecord);
            }
        }

        public bool ExistingRecords(HeaderInfo headerInfo)
        {
            try
            {
                string query = String.Format("select checksum from HeaderInfo a where a.Name = '{0}' and a.FirstObservation = '{1}' and a.LastObservation = '{2}'", headerInfo.Name, headerInfo.FirstObservation, headerInfo.LastObservation);
                SQLiteDataAdapter da = new SQLiteDataAdapter(query, sql_con);
                DataSet ds = new DataSet();
                da.Fill(ds, "HeaderInfo");
                if (ds.Tables["HeaderInfo"].Rows.Count == 1)
                {
                    DataRow drChecksum = ds.Tables["HeaderInfo"].Rows[0];
                    string checksum = drChecksum[0].ToString();
                    if (!checksum.Equals(headerInfo.Checksum))
                    {
                        SQLiteCommand commandTable = new SQLiteCommand(String.Format("delete from HeaderInfo where checksum = '{0}'", checksum), sql_con);
                        commandTable.ExecuteNonQuery();
                        return false;
                    }
                    LogWriter.WriteToLog(String.Format("Warning: already existing Checksum = {0} for Station = {1} from:{2} to {3}", checksum, headerInfo.Name, headerInfo.FirstObservation, headerInfo.LastObservation));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogWriter.WriteToLog(ex);
                LogWriter.WriteToLog(string.Format("Error at: Name:{0} First Observation:{1}", headerInfo.Name, headerInfo.FirstObservation));
                return false;
            }
        }

        private void CreateInsert(HeaderInfo headerInfo)
        {
            SQLiteCommand insertSQL = new SQLiteCommand("INSERT INTO HeaderInfo ('RinexVersion','RinexShortName','RinexLongName', 'Name', 'X', 'Y', 'Z', 'Intervall', 'FirstObservation', 'LastObservation', 'NumberOfObservations','NumberOfSatellites','Observer', 'ObserverType', 'ObserverNr', 'ObserverFirmware', 'GnssTimeSystem', 'AntennaType', 'AntennaNr', 'AntennaDeltaE', 'AntennaDeltaN', 'AntennaDeltaH', 'Agency', 'SatelliteSystem', 'CreatorInfo', 'CreatorName', 'CreationDateTime','GpsObservationTypes','GloObservationTypes','GalObservationTypes','BdsObservationTypes', 'Checksum', 'IsShop') VALUES (@param1,@param2,@param3,@param4,@param5,@param6,@param7,@param8,@param9,@param10,@param11,@param12,@param13,@param14,@param15,@param16,@param17,@param18,@param19,@param20,@param21,@param22,@param23,@param24,@param25,@param26,@param27,@param28,@param29,@param30,@param31,@param32,@param33)", sql_con);
            insertSQL.Parameters.Add(new SQLiteParameter("@param1", headerInfo.RinexVersion));
            insertSQL.Parameters.Add(new SQLiteParameter("@param2", headerInfo.RinexShortName));
            insertSQL.Parameters.Add(new SQLiteParameter("@param3", headerInfo.RinexLongname));
            insertSQL.Parameters.Add(new SQLiteParameter("@param4", headerInfo.Name));
            insertSQL.Parameters.Add(new SQLiteParameter("@param5", headerInfo.X));
            insertSQL.Parameters.Add(new SQLiteParameter("@param6", headerInfo.Y));
            insertSQL.Parameters.Add(new SQLiteParameter("@param7", headerInfo.Z));
            insertSQL.Parameters.Add(new SQLiteParameter("@param8", headerInfo.Intervall));
            insertSQL.Parameters.Add(new SQLiteParameter("@param9", headerInfo.FirstObservation));
            insertSQL.Parameters.Add(new SQLiteParameter("@param10", headerInfo.LastObservation));
            insertSQL.Parameters.Add(new SQLiteParameter("@param11", headerInfo.NumberOfObservations));
            insertSQL.Parameters.Add(new SQLiteParameter("@param12", headerInfo.NumberOfSatellites));
            insertSQL.Parameters.Add(new SQLiteParameter("@param13", headerInfo.Observer));
            insertSQL.Parameters.Add(new SQLiteParameter("@param14", headerInfo.ObserverType));
            insertSQL.Parameters.Add(new SQLiteParameter("@param15", headerInfo.ObserverNr));
            insertSQL.Parameters.Add(new SQLiteParameter("@param16", headerInfo.ObserverFirmware));
            insertSQL.Parameters.Add(new SQLiteParameter("@param17", headerInfo.GnssTimeSystem));
            insertSQL.Parameters.Add(new SQLiteParameter("@param18", headerInfo.AntennaType));
            insertSQL.Parameters.Add(new SQLiteParameter("@param19", headerInfo.AntennaNr));
            insertSQL.Parameters.Add(new SQLiteParameter("@param20", headerInfo.AntennaDeltaE));
            insertSQL.Parameters.Add(new SQLiteParameter("@param21", headerInfo.AntennaDeltaN));
            insertSQL.Parameters.Add(new SQLiteParameter("@param22", headerInfo.AntennaDeltaH));
            insertSQL.Parameters.Add(new SQLiteParameter("@param23", headerInfo.Agency));
            insertSQL.Parameters.Add(new SQLiteParameter("@param24", headerInfo.SatelliteSystem));
            insertSQL.Parameters.Add(new SQLiteParameter("@param25", headerInfo.CreatorInfo));
            insertSQL.Parameters.Add(new SQLiteParameter("@param26", headerInfo.CreatorName));
            insertSQL.Parameters.Add(new SQLiteParameter("@param27", headerInfo.CreationDateTime));
            insertSQL.Parameters.Add(new SQLiteParameter("@param28", headerInfo.GpsObservationTypes));
            insertSQL.Parameters.Add(new SQLiteParameter("@param29", headerInfo.GloObservationTypes));
            insertSQL.Parameters.Add(new SQLiteParameter("@param30", headerInfo.GalObservationTypes));
            insertSQL.Parameters.Add(new SQLiteParameter("@param31", headerInfo.BdsObservationTypes));
            insertSQL.Parameters.Add(new SQLiteParameter("@param32", headerInfo.Checksum));
            insertSQL.Parameters.Add(new SQLiteParameter("@param33", headerInfo.IsShop));
            try
            {
                insertSQL.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                LogWriter.WriteToLog(ex);
                LogWriter.WriteToLog(string.Format("Error at: Name:{0} First Observation:{1}", headerInfo.Name, headerInfo.FirstObservation));
            }
        }

        private void OptimizeDB()
        {
            string optimizeSQL = "PRAGMA optimize";
            SQLiteCommand commandTable = new SQLiteCommand(optimizeSQL, sql_con);
            commandTable.ExecuteNonQuery();
        }
        public void Dispose()
        {
            OptimizeDB();
            sql_con.Close();
            Dispose();
            GC.SuppressFinalize(this);
        }
    }
}