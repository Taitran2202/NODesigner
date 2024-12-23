using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Data
{
    public class UserContext : Microsoft.EntityFrameworkCore.DbContext
    {
        static string DataSourcePath;
        static UserContext()
        {
            DataSourcePath = System.IO.Path.Combine(MainWindow.AppPath, "user.db");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var conn = new SqliteConnection("Datasource="+ DataSourcePath);
            options.UseSqlite(conn);
           
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RolePermission>()
                .HasKey(bc => new { bc.RoleID, bc.PermissionID });
            modelBuilder.Entity<RolePermission>()
                .HasOne(bc => bc.Role)
                .WithMany(b => b.PermissionsLink)
                .HasForeignKey(bc => bc.RoleID);
            modelBuilder.Entity<RolePermission>()
                .HasOne(bc => bc.Permission)
                .WithMany(c => c.RolesLink)
                .HasForeignKey(bc => bc.PermissionID);
            modelBuilder.Entity<Permission>()
                .HasData(
                    new Permission
                    {
                        ID = 1,
                        Description = "Online",
                        PermissionKey = "btn_online"
                    },
                    new Permission
                    {
                        ID = 2,
                        Description = "Save",
                        PermissionKey = "btn_save"
                    },
                    new Permission
                    {
                        ID = 3,
                        Description = "Create Job",
                        PermissionKey = "btn_create"
                    },
                    new Permission
                    {
                        ID = 4,
                        Description = "Save Copy Job",
                        PermissionKey = "btn_save_as"
                    },
                    new Permission
                    {
                        ID = 5,
                        Description = "Statistics",
                        PermissionKey = "btn_statistics"
                    },
                    new Permission
                    {
                        ID = 6,
                        Description = "Add New Vision Model",
                        PermissionKey = "btn_add_new_vision"
                    },
                    new Permission
                    {
                        ID = 7,
                        Description = "Online",
                        PermissionKey = "btn_online"
                    },
                    new Permission
                    {
                        ID = 8,
                        Description = "Toolbar",
                        PermissionKey = "bar_file"
                    },
                    new Permission
                    {
                        ID = 9,
                        Description = "Open Designer",
                        PermissionKey = "CanOpenDesigner"
                    },
                    new Permission
                    {
                        ID = 10,
                        Description = "Open Records",
                        PermissionKey = "CanOpenRecords"
                    },
                    new Permission
                    {
                        ID = 11,
                        Description = "Open Menu",
                        PermissionKey = "CanOpenMenu"
                    },
                    new Permission
                    {
                        ID = 12,
                        Description = "Open Job Control",
                        PermissionKey = "btn_jobControl"
                    },
                    new Permission
                    {
                        ID = 13,
                        Description = "User Management",
                        PermissionKey = "btn_privilege"
                    }, 
                    new Permission
                    {
                        ID = 14,
                        Description = "Close Application",
                        PermissionKey = "btn_close_application"
                    }
                    );
        }
        public static UserContext Create()
        {
            return new UserContext();
        }
        public UserContext()
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserAction> Actions { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
    }
    public class UserAction
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public DateTime Time { get; set; }
        public string ActionType { get; set; }
        public string Property { get; set; }
        public string Target { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        [ForeignKey("UserID")]
        public User User { get; set; }
    }
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string UserName { get; set; }
        public string Hash { get; set; }
        public string Salt { get; set; }
        [ForeignKey("RoleID")]
        public Role Role { get; set; }
        public string Name { get; set; }
        public List<UserAction> Actions { get; set; } = new List<UserAction>();
        public DateTime LastLogin { get; set; }
        public DateTime DateCreated { get; set; }
        public string Description { get; set; }
    }
    public class RolePermission
    {
        //public int ID { get; set; }
        public int RoleID { get; set; }
        //[ForeignKey("RoleID")]
        public Role Role { get; set; }
        //[ForeignKey("PermissionID")]
        public int PermissionID { get; set; }
        public Permission Permission { get; set; }
    }
    public class HashSalt
    {
        public string Hash { get; set; }
        public string Salt { get; set; }
    }  
    public class Role
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string Name { get; set; }
        //[ForeignKey("RoleID")]
        public List<RolePermission> PermissionsLink { get; set; } = new List<RolePermission>();
    }
    public class Permission
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string Description { get; set; }
        public string PermissionKey { get; set; }
        //[ForeignKey("PermissionID")]
        public List<RolePermission> RolesLink { get; set; } = new List<RolePermission>();
    }
    
}
