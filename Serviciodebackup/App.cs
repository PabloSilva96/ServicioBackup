using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using WinSCP;

namespace Serviciodebackup
{
    class App
    {

        public static void app()
        {

            var myJsonString = File.ReadAllText(".\\appsettings.json");
            var myJObject = JObject.Parse(myJsonString);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string dayofWeek = myJObject.SelectToken("$.Time.DayOfTheWeek").Value<string>();

            // validar o dia da semana no que se queren facer as copias 
            if (dayofWeek == "*")
            {
                doBackup();
            }
            else if (dayofWeek.Contains(","))
            {
                string[] days = dayofWeek.Split(",");
                foreach (var day in days)
                {
                    if (Int32.Parse(day) == (int)DateTime.Now.DayOfWeek)
                    {
                        doBackup();
                    }
                }
            }
            else if (Int32.Parse(dayofWeek) == (int)DateTime.Now.DayOfWeek)
            {
                doBackup();
            }
            else
            {
                WriteEventLogEntry("Error: the format of DauOfTheWeek in appsettings.json must be one or various numbers between 0 and 6 separated with comas");
            }

            void doBackup()
            {
                WriteEventLogEntry("Backups starts");
                DirectoryInfo di = Directory.CreateDirectory(".\\backups");

                string jsonConnection = myJObject.SelectToken("$.MySQL.Connection").Value<string>();
                string excludedDatabases = myJObject.SelectToken("$.MySQL.ExcludeDatabases").Value<string>();
                string includedDatabases = myJObject.SelectToken("$.MySQL.IncludeDatabases").Value<string>();
                Mysql.mysqlBackup(jsonConnection, excludedDatabases, includedDatabases);

                string dirSource = myJObject.SelectToken("$.Dir.Source").Value<string>();
                string dirTemp = ".\\backups\\dirbackup";
                string sourceDirectoryName = Path.GetFileName(dirSource);
                string destiny = myJObject.SelectToken("$.Dir.Destiny").Value<string>();
                string excluded = myJObject.SelectToken("$.Dir.ExcludeExtensions").Value<string>();
                string included = myJObject.SelectToken("$.Dir.IncludeExtensions").Value<string>();
                Dir.dirBackup(dirSource, dirTemp, excluded, included);

                // debido a problemas relacionados con los Times de transferencia con el servidor sql se optó por realizar los backups directamente desde el servidor y copiarlos desde ahi
                string mssqlSource = myJObject.SelectToken("$.MSSQL.BackupDirectory").Value<string>();
                string mssqlTemp = ".\\backups\\mssqlbackup";
                Dir.dirBackup(mssqlSource, mssqlTemp, excluded, included);

                string compressExtension = myJObject.SelectToken("$.Dir.Compress").Value<string>();
                // si o final o destino e por WinSCP deberia cambiar as miñas variables destiny e comprimir en .\\backups para despois borralo
                var mysqlzip = destiny + "\\Mysql" + timestamp + compressExtension;
                var dirzip = destiny + "\\" + sourceDirectoryName + timestamp + compressExtension;
                var mssqlzip = destiny + "\\MSSQL" + timestamp + compressExtension;

                try
                {
                    ZipFile.CreateFromDirectory(".\\backups\\mysqlbackup", mysqlzip);                  
                    ZipFile.CreateFromDirectory(".\\backups\\dirbackup", dirzip);
                    ZipFile.CreateFromDirectory(".\\backups\\mssqlbackup", mssqlzip);
                    
                    CopyWithWinSCP(mysqlzip, dirzip, mssqlzip);

                    Directory.Delete(".\\backups", true);
                }
                catch (Exception e)
                {
                    // si ocurre algun error relacionado cos ficheros ou directorios
                    App.WriteEventLogEntry("Error " + e.Message);
                }

                WriteEventLogEntry("Backup realizado con éxito");
            }

            void CopyWithWinSCP(string mysqlzip, string dirzip, string mssqlzip)
            {
                SessionOptions sessionOptions = new SessionOptions();
                sessionOptions.Protocol = Protocol.Ftp;
                sessionOptions.HostName = "aaa";
                sessionOptions.UserName = "wwww";
                sessionOptions.Password = "contraseña";
                sessionOptions.FtpSecure = FtpSecure.Explicit;
                sessionOptions.TlsHostCertificateFingerprint = "eso";
                Session session = new Session();
                session.Open(sessionOptions);
                TransferOptions transferOptions = new TransferOptions();
                transferOptions.TransferMode = TransferMode.Binary;
                TransferOperationResult transferResult;
                transferResult = session.PutFiles(mysqlzip, "/", false, transferOptions);
                transferResult = session.PutFiles(dirzip, "/", false, transferOptions);
                transferResult = session.PutFiles(mssqlzip, "/", false, transferOptions);
                transferResult.Check();
            }
        }

 //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////       

        public static int getMinutes()
        {
            var myJsonString = File.ReadAllText(@"C:\backupservice\appsettings.json");
            var myJObject = JObject.Parse(myJsonString);
            int minutes = myJObject.SelectToken("$.Time.Minutes").Value<int>();
            return minutes;
        }

        public static void WriteEventLogEntry(string message)
        {
            if (!EventLog.SourceExists("MyBackupservice"))
            {
                EventLog.CreateEventSource("MyBackupservice", "Backupservice");
            }
            EventLog mylog = new EventLog();
            mylog.Source = "MyBackupservice";
            // cando salta unha excepción escribo no lol Error + mensaje da excepción como un error
            // e si hay algo mal nas settings como que se excluya unha base que se quere incluir poño Aviso ...
            if (message.Contains("Error"))
            {
                mylog.WriteEntry(message, EventLogEntryType.Error);
            }
            else if (message.Contains("Warning"))
            {
                mylog.WriteEntry(message, EventLogEntryType.Warning);
            }
            else
            {
                mylog.WriteEntry(message, EventLogEntryType.Information);
            }
            mylog.Close();
        }
    }
}
