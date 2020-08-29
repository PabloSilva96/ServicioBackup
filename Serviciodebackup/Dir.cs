
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Serviciodebackup
{
    class Dir
    {
        public static void dirBackup(string source, string temp, string excluded, string included)
        {
            DirectoryInfo di = Directory.CreateDirectory(temp);
            string[] excludedExtensions = excluded.Split(' ');
            string[] includedExtensions = included.Split(' ');
            string[] subdirectories = Directory.GetDirectories(source, "*", SearchOption.TopDirectoryOnly);
            if (!Directory.Exists(source))
            {
                App.WriteEventLogEntry("Error, Check that the directory path you want to backup is correct");
            }
            else
            {
                // non se me ocurre maneira de meter esto en un bucle que se ejecute o número de veces que niveles queira avanzar
                excludeExtensions(source);
                Xcopy(source, temp);
                
                foreach (var subdirectory in subdirectories)
                {
                    excludeExtensions(subdirectory);// esto borra e escribe cada vez no mismo ficheiro asiq e pouco eficaz
                    createDirectoryAndCopy(subdirectory);
                    string[] subsubdirectories = Directory.GetDirectories(subdirectory, "*", SearchOption.TopDirectoryOnly);
                    foreach (var subsubdirectory in subsubdirectories)
                    {
                        // poderia meter oitro array chamado subsubsubdirectories pero igual hay algunha maneira mellor de facelo  e esto xa cubre 3 niveles 
                        excludeExtensions(subsubdirectory);
                        createDirectoryAndCopy(subsubdirectory);
                    }
                }
                
            }

            void excludeExtensions(string path)
            {
                // si hay algo distinto de * en included ignoro completamente o que este escrito en excluded e excluyo todo menos o que quero incluir 
                if (!included.Equals("*"))
                {
                    string[] directoryExtensions = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Select(Path.GetExtension).ToArray();
                    //en notIncludedExtensions se gardan as extensions que se queiran excluir(todas menos as incluidas), sean unicas e esten no directortio origen 
                    IEnumerable<string> notIncludedExtensions = directoryExtensions.Except(includedExtensions);
                    //ten que ser co WriteLine porque e necesario añadirlle a \ o final pero si a poño no JSON non podo buscar por extensions 
                    using (StreamWriter writer = new StreamWriter(".\\excluded.txt"))
                    {
                        foreach (var items in notIncludedExtensions)
                        {
                            writer.WriteLine(items.ToString() + "\\");
                        }
                    }
                }
                else
                {
                    System.IO.File.WriteAllLines(".\\excluded.txt", excludedExtensions);
                }
            }
            void createDirectoryAndCopy(string subdirectory)
            {
                // esto e necesario para manter a estructura de ficheros e directorios
                string subdirectoryName = subdirectory.Replace(source, string.Empty);
                string subdirectoryDestiny = temp + subdirectoryName;
                DirectoryInfo di = Directory.CreateDirectory(subdirectoryDestiny);
                Xcopy(subdirectory, subdirectoryDestiny); // mala idea ter un metodo que chama un metodo? seguramente si
            }

            void Xcopy(string sourcePath, string destinyPath)
            {
                try
                {
                    using (Process xcopy = new Process())
                    {
                        xcopy.StartInfo.FileName = "xcopy";
                        //  \e para que copie todos os subdirectorios
                        xcopy.StartInfo.Arguments = "\"" + sourcePath + "\"" + " " + "\"" + destinyPath + "\"" + " /i /y /exclude:.\\excluded.txt";
                        xcopy.StartInfo.CreateNoWindow = true;
                        xcopy.StartInfo.ErrorDialog = false;
                        xcopy.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        xcopy.Start();
                        xcopy.WaitForExit();
                        xcopy.Close();
                    }
                }
                catch (Exception e)
                {
                    App.WriteEventLogEntry("Error "+ e.Message);
                }
            }
        }
    }
}
