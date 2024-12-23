using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NOVisionDesigner.Data
{
    public class RecordFolderHelper
    {
        static RecordFolderHelper()
        {
            //create database if not exist.
            using (var context = new RecordFolderAccessContext())
            {
                context.Database.EnsureCreated();
            }

        }
        public void InsertOrUpdate(string Path,DateTime date)
        {
            using(var context = new RecordFolderAccessContext())
            {
                var selected = context.Record.FirstOrDefault(x => x.Path == Path);
                if(selected != null)
                {
                    selected.Time = date;

                }
                else
                {
                    context.Record.Add(new RecordFolderData() { Path = Path, Time = date });
                }
                context.SaveChanges();
            }
        }
        public void CleanDirectory(int DayToKeep=30)
        {
            Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                using (var context = new RecordFolderAccessContext())
                {

                    var Lastdate = DateTime.Now - TimeSpan.FromDays(DayToKeep);
                    var selected = context.Record.Where(x => x.Time < Lastdate);
                    foreach (var item in selected)
                    {
                        try
                        {
                            
                            if (System.IO.Directory.Exists(item.Path)){
                                Console.WriteLine("Begin delete folder: " + item.Path);
                                System.IO.Directory.Delete(item.Path, true);
                                Console.WriteLine("Complete delete folder: " + item.Path);
                            }
                            
                            
                            context.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                            context.SaveChanges();
                        }
                        catch(Exception ex)
                        {

                        }
                        
                        
                    }
                    

                }
            });
            
        }
    }
}
