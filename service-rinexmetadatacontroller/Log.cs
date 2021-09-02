using System;

namespace RinexMetaDataController
{

    public class Log
    {

        private string LogMessage;
        private DateTime LogTime;

        public Log(string message)
        {
            LogMessage = message;
            LogTime = DateTime.Now;
        }

        public string GetMessage()
        {
            return LogMessage;
        }

        public string GetTime()
        {
            return LogTime.ToString("HH:mm:ss.fff");
        }

        public string GetDate()
        {
            return LogTime.ToString("yyyy-MM-dd");
        }
    }
}