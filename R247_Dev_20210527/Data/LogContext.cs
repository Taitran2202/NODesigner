using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Data
{
    public class LogContext : Microsoft.EntityFrameworkCore.DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var conn = new SqliteConnection("Datasource=log.db");
            options.UseSqlite(conn);
        }

        public DbSet<LogData> Log { get; set; }
    }
    public enum LogType
    {
        ERROR = 0, INFO = 1, WARNING = 2
    }
    public class LogData
    {
        public int Id { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public LogType Type { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
        public string Source { get; set; }
    }
}
