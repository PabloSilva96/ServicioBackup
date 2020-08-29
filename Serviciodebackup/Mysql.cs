using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace Serviciodebackup
{
    class Mysql
    {
        public static void mysqlBackup(string jsonConnection, string excludedDatabases, string includedDatabases)
        {
            DirectoryInfo di = Directory.CreateDirectory(".\\backups\\mysqlbackup");

            string connectionString;

            if (jsonConnection.Equals("DefaultConnection"))
            {
                // igual esta non deberia ser a conexion por defecto
                connectionString = "";
            }
            else
            {
                connectionString = jsonConnection;
            }

            if (excludedDatabases.Contains(includedDatabases))
            {
                App.WriteEventLogEntry("Warning: one or more of the databases planned to be included are going to be excluded, check your settings and verify your included/excluded databases");
            }

            try
            {
                var connection = new MySqlConnection(connectionString);
                string credentialspath = ".\\dump.sqlpwd";
                string databaseName = " ";
                string tableName;
                string filetoExecute = ".\\mysqldump.exe";
                connection.Open(); // igual deberia abrir a conexion de maneira asincrona

                string sql = "select table_schema as database_name, table_name from information_schema.tables where table_type = 'BASE TABLE'and table_schema rlike '" +
                    includedDatabases + "' and table_schema not in (" + excludedDatabases + ") order by database_name, table_name; ";
                MySqlCommand command = new MySqlCommand(sql, connection);
                DataTable data = new DataTable();
                var adapter = new MySqlDataAdapter(command);
                adapter.Fill(data);
                foreach (DataRow row in data.Rows)
                {
                    // para ter en unha variable o nombre da base de datos que estou usando en ese momento 
                    if (databaseName != (string)row[0])
                    {
                        databaseName = (string)row[0];
                    }

                    tableName = (string)row[1];
                    DirectoryInfo dio = Directory.CreateDirectory(".\\backups\\mysqlbackup\\" + databaseName);
                    string Arguments = " --defaults-extra-file=" + credentialspath + " --column-statistics=0 --single-transaction " +
                        databaseName + " " + tableName + " --result-file .\\backups\\mysqlbackup\\" + databaseName + "\\" + tableName + ".sql";

                    using (Process MysqlDump = new Process())
                    {

                        MysqlDump.StartInfo.UseShellExecute = false;
                        MysqlDump.StartInfo.FileName = filetoExecute;
                        MysqlDump.StartInfo.Arguments = Arguments;
                        MysqlDump.StartInfo.CreateNoWindow = true;
                        MysqlDump.StartInfo.ErrorDialog = false;
                        MysqlDump.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        MysqlDump.StartInfo.RedirectStandardError = true;
                        MysqlDump.StartInfo.RedirectStandardInput = true;
                        // Non se pode redirigir o output e o error anonser que se lea por o menos 1 de maneira asincrona
                        //MysqlDump.StartInfo.RedirectStandardOutput = true;
                        MysqlDump.Start();
                        MysqlDump.WaitForExit();
                        MysqlDump.Close();
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                // si fallo algo de mysql que non sea o dump escribe este mensaje no log
                App.WriteEventLogEntry("Error " + e.Message);
            }

        }
    }
}
