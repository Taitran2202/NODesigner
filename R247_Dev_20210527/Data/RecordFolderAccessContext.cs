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
    public class RecordFolderAccessContext : Microsoft.EntityFrameworkCore.DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var conn = new SqliteConnection("Datasource=RecordFolder.db");
            options.UseSqlite(conn);
        }

        public DbSet<RecordFolderData> Record { get; set; }
    }
    public class RecordFolderData
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public string Path { get; set; }
    }
}
