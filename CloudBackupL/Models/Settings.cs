﻿using SQLite;
using System;
using System.Text;

namespace CloudBackupL.Models
{
    class Settings
    {
        [PrimaryKey, AutoIncrement, NotNull]
        public int id { get; set; }

        public byte[] password { get; set; }

        [NotNull]
        public string trayType { get; set; }

        public bool askPassword { get; set; }

        public bool preventShutDown { get; set; }

        public int chunkSize { get; set; }

        public int nrAttempts { get; set; }

        public int delay { get; set; }

        [NotNull MaxLength(250)]
        public string logFileLocation { get; set; }

        public string getPassword()
        {
            return password != null ? Encoding.UTF8.GetString(password) : null;
        }

        public void setPassword(string pass)
        {
            password = Encoding.UTF8.GetBytes(pass);
        }
    }
}
