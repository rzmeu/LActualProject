﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using SQLite;
using System.IO;

namespace CloudBackupL
{
    class DatabaseService
    {
        String connString = ConfigurationManager.AppSettings["DbSQLite"];

        public DatabaseService()
        {
            if (!File.Exists(connString))
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString, true))
                {
                    conn.CreateTable<Cloud>();
                    conn.CreateTable<BackupPlan>();
                    conn.CreateTable<Backup>();
                }
            }
            
        }

        public int InsertBackupPlan(BackupPlan backupPlan)
        {
            int key;
            using(SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                key = conn.Insert(backupPlan);
            }
            return key;
        }

        public int UpdateBackupPlan(BackupPlan backupPlan)
        {
            int key;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                key = conn.Update(backupPlan);
            }
            return key;
        }

        public int UpdateCloud(Cloud cloud)
        {
            int key;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                key = conn.Update(cloud);
            }
            return key;
        }

        public int InsertCloud(Cloud cloud)
        {
            int key;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                key = conn.Insert(cloud);
            }

            return key;
        }

        public void InsertBackup(Backup backup)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                conn.Insert(backup);
            }

            BackupPlan backupPlan = GetBackupPlan(backup.backupPlanId);
            backupPlan.lastExecution = backup.date;
            backupPlan.currentStatus = "Updated";
            backupPlan.lastResult = true;
            backupPlan.lastBackupDuration = backup.runTime;

            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                conn.Update(backupPlan);
            }

        }

        public List<Cloud> GetAllClouds()
        {
            List<Cloud> result;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                result = conn.Query<Cloud>("select * from cloud");
            }
            return result;
        }

        public List<BackupPlan> GetAllPlans()
        {
            List<BackupPlan> result;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                result = conn.Query<BackupPlan>("select * from BackupPlan");
            }
            return result;
        }

        public bool CheckPlanName(String name)
        {
            int count = 1;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                count = conn.Query<Cloud>("select * from cloud where name = ?", name).Count;
            }
            return count == 0 ? true : false;
        }

        public void DeletePlan(int id)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                conn.Delete<BackupPlan>(id);
                conn.Query<Backup>("delete from Backup where backupPlanId = ?", id);
            }
        }

        public BackupPlan GetBackupPlan(int id)
        {
            BackupPlan plan = null;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                plan = conn.Get<BackupPlan>(id);
            }
            return plan;
        }

        public Cloud GetCloudByName(String name)
        {
            List<Cloud> cloud;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                cloud = conn.Query<Cloud>("select * from cloud where name = ?", name);
            }
            return cloud[0];
        }

        public bool IsCloudAlreadyInsered(string id)
        {
            int count = 0;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                count = conn.Query<Cloud>("select * from cloud where id = ?", id).Count;
            }
            return count > 0 ? true : false;
        }

        public bool CanDeleteCloud(string id)
        {
            int count = 0;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                count = conn.Query<Cloud>("select * from BackupPlan where cloudId = ?", id).Count;
            }
            return count > 0 ? false : true;
        }

        public void DeleteCloud(string id)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                conn.Delete<Cloud>(id);
            }
        }

        public void DeleteBackup(int id)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                conn.Delete<Backup>(id);
            }
        }

        public List<Backup> GetBackCloudBackups(string cloudId)
        {
            List<Backup> result;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                result = conn.Query<Backup>("select * from Backup where cloudId = ?", cloudId);
            }
            return result;
        }

        public Backup GetBackup(int backupId)
        {
            Backup result;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                result = conn.Get<Backup>(backupId);
            }
            return result;
        }

        public Backup GetLastBackup(int planId)
        {
            List<Backup> result;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                result = conn.Query<Backup>("select * from Backup where backupPlanId = ? order by id desc limit 1", planId);
            }
            return result.Count > 0 ? result[0] : null;
        }

        public Cloud GetCloud(string cloudId)
        {
            Cloud result;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                result = conn.Get<Cloud>(cloudId);
            }
            return result;
        }

        public List<Backup> GetBackupsByPlanId(int planId)
        {
            List<Backup> result;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                result = conn.Query<Backup>("select * from Backup where backupPlanId = ?", planId);
            }
            return result;
        }

        public Cloud GetCloudByToken(string accessToken)
        {
            List<Cloud> clouds;
            using (SQLiteConnection conn = new SQLiteConnection(connString, true))
            {
                clouds = conn.Query<Cloud>("select * from Cloud where token = ?", accessToken);
            }
            return clouds.Count == 1 ? clouds[0] : null;
        }

    }
}
