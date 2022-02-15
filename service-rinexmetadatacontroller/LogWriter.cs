using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace RinexMetaDataController
{
    public class LogWriter
    {
        private static LogWriter Instance;
        private static Queue<Log> LogQueue;
        private static string LogPath = ConfigurationManager.AppSettings["LogPath"];
        private static string LogFile = ConfigurationManager.AppSettings["LogFile"];
        private static int FlushAtAge = int.Parse(ConfigurationManager.AppSettings["FlushAtAge"]);
        private static int FlushAtQty = int.Parse(ConfigurationManager.AppSettings["FlushAtQty"]);
        private static bool showWarning = true;

        private static DateTime FlushedAt;

        private LogWriter()
        {
            bool.TryParse(ConfigurationManager.AppSettings["ShowWarning"], out showWarning);
        }

        public static LogWriter GetInstance
        {
            get
            {
                if (Instance == null)
                {
                    Instance = new LogWriter();
                    LogQueue = new Queue<Log>();
                    FlushedAt = DateTime.Now;
                }
                return Instance;
            }
        }

        public static void WriteToLog(string message)
        {
            lock (LogQueue)
            {
                Log log = new Log(message);
                LogQueue.Enqueue(log);
                if (LogQueue.Count >= FlushAtQty || CheckTimeToFlush())
                {
                    FlushLogToFile();
                }
            }
        }

        public static void WriteToLog(Exception e)
        {
            lock (LogQueue)
            {

                // Create log
                Log msg = new Log("Error: " + e.Source.ToString().Trim() + " " + e.Message.ToString().Trim());
                Log stack = new Log("Stack: " + e.StackTrace.ToString().Trim());
                LogQueue.Enqueue(msg);
                LogQueue.Enqueue(stack);

                // Check if should flush
                if (LogQueue.Count >= FlushAtQty || CheckTimeToFlush())
                {
                    FlushLogToFile();
                }

            }
        }

        public static void ForceFlush()
        {
            FlushLogToFile();
        }

        private static bool CheckTimeToFlush()
        {
            TimeSpan time = DateTime.Now - FlushedAt;
            if (time.TotalSeconds >= FlushAtAge)
            {
                FlushedAt = DateTime.Now;
                return true;
            }
            return false;
        }

        private static void FlushLogToFile()
        {
            Stream stream = null;
            while (LogQueue.Count > 0)
            {
                Log entry = LogQueue.Dequeue();
                string path = Path.Combine(LogPath, entry.GetDate() + "_" + LogFile);
                stream = new FileStream(path, FileMode.Append, FileAccess.Write);
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    if (showWarning)
                    {
                        writer.WriteLine(String.Format(@"{0}: {1}", entry.GetTime(), entry.GetMessage()));
                    }
                    else
                    {
                        if (!entry.GetMessage().Contains("Warning"))
                        {
                            writer.WriteLine(String.Format(@"{0}: {1}", entry.GetTime(), entry.GetMessage()));
                        }
                    }
                }
            }
        }
    }
}
