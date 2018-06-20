using System;
using System.Configuration;
using System.Net;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace EventStorePersistentSubscriber
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard //I disabled this
        // and AzureWebJobsStorage //for saving console/logging output
        static void Main()
        {
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;

            using (var loggerFactory = new LoggerFactory())
            {
                var config = new JobHostConfiguration();
                var instrumentationKey =
                    ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"];
                config.DashboardConnectionString = "";
                config.LoggerFactory = loggerFactory
                    .AddApplicationInsights(instrumentationKey, null)
                    .AddConsole();

                var host = new JobHost(config);
                host.Call(typeof(Functions).GetMethod("ProcessMethod")); //thread blocked in here- if it falls over want azure to report failure/ restart

                throw new AppDomainUnloadedException($"{typeof(Program).Assembly.GetName().Name} - WebJob has terminated");
                //host.RunAndBlock(); //will block thread and wait for events, we don't not interested in events or schedules
            }
        }
    }
}
