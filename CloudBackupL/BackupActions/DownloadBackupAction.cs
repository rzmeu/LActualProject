﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using CloudBackupL.Utils;
using Ionic.Zip;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace CloudBackupL.BackupActions
{

    class DownloadBackupAction
    {
        BackgroundWorker backgroundWorker = new BackgroundWorker();
        string tempDownloadZipFolder = AppDomain.CurrentDomain.BaseDirectory + "tmpZipDownloadFolder\\";
        DatabaseService databaseService = new DatabaseService();
        BackupPlan backupPlan;
        Backup backup;
        Label labelStatus;
        ProgressBar progressBar;
        ICloud cloudController;
        Cloud cloud;
        EventHandler<Boolean> downloadCompleteEvent;
        string downloadPath;
        string requestFilesListResult;
        TaskCompletionSource<bool> tastWaitDownload;
        string password;

        public DownloadBackupAction(Backup backup, Label labelStatus, ProgressBar progressBar, string downloadPath, EventHandler<Boolean> downloadCompleteEvent, string password)
        {
            this.backup = backup;
            this.labelStatus = labelStatus;
            this.progressBar = progressBar;
            this.downloadPath = downloadPath;
            this.downloadCompleteEvent = downloadCompleteEvent;
            this.password = password;
            backupPlan = databaseService.GetBackupPlan(backup.backupPlanId);
            cloud = databaseService.GetCloud(backupPlan.cloudId);
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            if(cloud.cloudType.Equals("dropbox"))
            {
                cloudController = new DropBoxController();
            }
        }

        public void StartDownloadBackupAction()
        {
            if (backgroundWorker.IsBusy == false)
            {
                backgroundWorker.RunWorkerAsync();
            }
        }
        
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            cloudController.GetFilesList(cloud.token, LoadFilesCallback, backup.targetPath);
            tastWaitDownload = new TaskCompletionSource<bool>();
            tastWaitDownload.Task.Wait();

            Directory.CreateDirectory(tempDownloadZipFolder);
            int i = 1;
            dynamic files = JObject.Parse(requestFilesListResult);
            int totalFiles = ((ICollection)files.contents).Count;
            foreach (var file in files.contents)
            {
                var tcs = new TaskCompletionSource<bool>();
                labelStatus.Invoke(new Action(() => labelStatus.Text = "Downloading file " + i + " of " + totalFiles));
                progressBar.Invoke(new Action(() => progressBar.Value = 0));
                string fileName = Path.GetFileName((string)file.path);

                cloudController.Download((string)file.path, cloud.token, tempDownloadZipFolder + fileName, Web_DownloadProgressChanged, tcs);
                tcs.Task.Wait();
                i++;
            }
            labelStatus.Invoke(new Action(() => labelStatus.Text = ""));
            progressBar.Invoke(new Action(() => progressBar.Value = 0));

            //now save file to directory
            ArchiveUtils.ExtractZip(tempDownloadZipFolder, downloadPath, Zip_ExtractProgress, password);

            labelStatus.Invoke(new Action(() => labelStatus.Text = ""));
            progressBar.Invoke(new Action(() => progressBar.Value = 0));
            ArchiveUtils.DeleteDirectory(tempDownloadZipFolder);
        }

        private void Web_DownloadProgressChanged(object sender, object e)
        {
            DownloadProgressChangedEventArgs args = (DownloadProgressChangedEventArgs)e;
            progressBar.Invoke(new Action(() => progressBar.Value = args.ProgressPercentage));
        }


        private void LoadFilesCallback(object sender, DownloadStringCompletedEventArgs e)
        {
            if(e.Error == null)
            {
                requestFilesListResult = e.Result;
                tastWaitDownload.TrySetResult(true);
            } else
            {
                tastWaitDownload.TrySetException(new Exception("Error getting file list"));
            }
        }


        static int currentEntryNr = 1;
        private void Zip_ExtractProgress(object sender, object e)
        {
            ZipProgressEventArgs args = (ZipProgressEventArgs)e;
            if (args.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
            {
                int progress = (int)((args.BytesTransferred * 100) / args.TotalBytesToTransfer);
                progressBar.Invoke(new Action(() => progressBar.Value = progress));
            }
            if (args.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
            {
                labelStatus.Invoke(new Action(() => labelStatus.Text = "Status: Extracting file " + currentEntryNr + " of " + args.EntriesTotal));
                currentEntryNr++;
            }
            if(args.EventType == ZipProgressEventType.Extracting_AfterExtractAll)
            {
                currentEntryNr = 1;
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Error == null)
            {
                downloadCompleteEvent(this, true);
            } else
            {
                downloadCompleteEvent(this, false);
            }    
        }

    }
}