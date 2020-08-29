using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.IO;
//main function
namespace Serviciodebackup
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Cando se ejecuta como servicio o directorio actual pasa a ser System32, esto se encarga de que sea o directorio no que esta o ejecutable do programa
            var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
            var pathToContentRoot = Path.GetDirectoryName(pathToExe);
            Directory.SetCurrentDirectory(pathToContentRoot);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddHostedService<Worker>();
        }).UseWindowsService();
    }
}
