using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Data
{
    public class LogProvider 
    {
        public EventHandler OnLog;
        static LogProvider()
        {
            //create database if not exist.
            using (var context = new LogContext())
            {
                context.Database.EnsureCreated();
            }

        }
        public void LogError(string source, string message)
        {
            
            try
            {
                var newlog = new LogData() { Type = LogType.ERROR, Message = message, Source = source, Time = DateTime.Now };
                using (var context = new LogContext())
                {
                    
                    context.Log.Add(newlog);
                    context.SaveChanges();
                }
                OnLog?.Invoke(newlog, null);
            }
            catch (Exception ex)
            {

            }

            

        }

        public void LogInfo(string source, string message)
        {
            try
            {
                var newlog = new LogData() { Type = LogType.INFO, Message = message, Source = source, Time = DateTime.Now };
                using (var context = new LogContext())
                {
                    context.Log.Add(newlog);
                    context.SaveChanges();
                }
                OnLog?.Invoke(newlog, null);
            }
            catch (Exception ex)
            {

            }


        }

        public void LogWarning(string source, string message)
        {
            try
            {
                var newlog = new LogData() { Type = LogType.WARNING, Message = message, Source = source, Time = DateTime.Now };
                using (var context = new LogContext())
                {
                    context.Log.Add(newlog);
                    context.SaveChanges();
                }
                OnLog?.Invoke(newlog, null);
            }
            catch (Exception ex)
            {

            }


        }
    }

    
}
