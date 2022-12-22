using GM.Application.Console.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using GM.ClientAPI;
using GM.ClientAPI.Endpoint;

namespace GM.Application.Console
{
    public class Program
    {
        private IConfiguration Configuration { get; set; }
        private static string _errorMsg { get; set; }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    throw new Exception("Arguments Not Found");
                }

                #region init
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                string strVersion = version.Major + "." + version.Minor + "." + version.Build + "." + version.Revision;
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                // create service collection
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection, args);

                // create service provider
                var serviceProvider = serviceCollection.BuildServiceProvider();

                IConsoleService consoleService = serviceProvider.GetService<IConsoleService>();

                string inputDate = consoleService.GetInputDate();
                string function = consoleService.GetFunction();

                consoleService.WriteLogs("");
                consoleService.WriteLogs("---------- Program Start -------------");
                consoleService.WriteLogs("Console Version  : " + strVersion);
                consoleService.WriteLogs("Received Command : " + function);
                consoleService.WriteLogs("Received Date    : " + inputDate);

                #endregion
                
                //update run date in checking eod
                consoleService.UpdateCheckingEod();

                switch (function)
                {
                    case "ExportDWHTHBDailyToSFTP":
                        serviceProvider.GetService<ExportDWHIAS39ToSFTP>().Run("Daily", "THB");
                        break;
                    case "ExportDWHTHBMonthlyToSFTP":
                        serviceProvider.GetService<ExportDWHIAS39ToSFTP>().Run("Monthly", "THB");
                        break;
                    case "ExportDWHIAS39DailyToSFTP":
                        serviceProvider.GetService<ExportDWHIAS39ToSFTP>().Run("Daily", "FCY");
                        break;
                    case "ExportDWHIAS39MonthlyToSFTP":
                        serviceProvider.GetService<ExportDWHIAS39ToSFTP>().Run("Monthly", "FCY");
                        break;
                    case "ExportDMSDataSetToSFTP":
                        serviceProvider.GetService<ExportDMSDataSetToSFTP>().Run();
                        break;
                    case "ExportDMSDataSetMonthlyToSFTP":
                        serviceProvider.GetService<ExportDMSDataSetMonthlyToSFTP>().Run();
                        break;
                    case "ExportFxReconcileToSFTP":
                        serviceProvider.GetService<ExportFxReconcileToSFTP>().Run();
                        break;
                    case "ExportNesToSFTP":
                        serviceProvider.GetService<ExportNesToSFTP>().Run();
                        break;
                    case "ExportGlToSFTP":
                        serviceProvider.GetService<ExportGlToSFTP>().Run();
                        break;
                    case "ExportFITSTrpToSFTP":
                        serviceProvider.GetService<ExportFITSTrpToSFTP>().Run();
                        break;
                    case "ExportAmendCancelDailyToEmail":
                        serviceProvider.GetService<ExportAmendCancelDailyToEmail>().Run();
                        break;
                    case "ExportAmendCancelMonthlyToEmail":
                        serviceProvider.GetService<ExportAmendCancelMonthlyToEmail>().Run();
                        break;
                    case "InterfaceFITSBondPledge":
                        serviceProvider.GetService<InterfaceFITSBondPledge>().Run();
                        break;
                    case "InterfaceEQUITYPledge":
                        serviceProvider.GetService<InterfaceEQUITYPledge>().Run();
                        break;
                    case "InterfaceCRTransLimit":
                        serviceProvider.GetService<InterfaceCRTransLimit>().Run("");
                        break;
                    case "InterfaceCRTransLimitEod":
                        serviceProvider.GetService<InterfaceCRTransLimit>().Run("EOD");
                        break;
                    case "InterfaceCCM":
                        serviceProvider.GetService<InterfaceCCM>().Run();
                        break;
                    case "InterfaceBBGMarketPrice":
                        serviceProvider.GetService<InterfaceBBGMarketPrice>().Run();
                        break;
                    case "InterfaceBBGMarketPriceTbmaT1":
                        serviceProvider.GetService<InterfaceMarketPriceTbmaFits>().Run("T1");
                        break;
                    case "InterfaceBBGMarketPriceTbmaT2":
                        serviceProvider.GetService<InterfaceMarketPriceTbmaFits>().Run("T2");
                        break;
                    case "InterfaceEQUITYNavPrice":
                        serviceProvider.GetService<InterfaceEQUITYNavPrice>().Run();
                        break;
                    case "InterfaceSSMDExchangeRate":
                        serviceProvider.GetService<InterfaceSSMDExchangeRate>().Run();
                        break;
                    case "InterfaceSSMDFloatingIndex":
                        serviceProvider.GetService<InterfaceSSMDFloatingIndex>().Run();
                        break;
                    case "InterfaceEXRATECounterRate":
                        serviceProvider.GetService<InterfaceEXRATECounterRate>().Run();
                        break;
                    case "InterfaceEXRATEMidRate":
                        serviceProvider.GetService<InterfaceEXRATEMidValuationRate>().Run("MIDRATE");
                        break;
                    case "InterfaceEXRATEValuationRate":
                        serviceProvider.GetService<InterfaceEXRATEMidValuationRate>().Run("VALUATIONRATE");
                        break;
                    case "InternalBatchJobEod":
                        serviceProvider.GetService<InternalBatchJobEod>().Run();
                        break;
                    case "InternalBatchJobEndOfDay":
                        serviceProvider.GetService<InternalBatchJobEndOfDay>().Run();
                        break;
                    case "InternalBatchJobCheckEod":
                        serviceProvider.GetService<InternalBatchJobCheckEod>().Run();
                        break;
                    case "ExportUserProfileMonthlyToEmail":
                        serviceProvider.GetService<ExportUserProfileMonthlyToEmail>().Run();
                        break;
                    case "InterfaceSSMDThorRate":
                        serviceProvider.GetService<InterfaceSSMDThorRate>().Run();
                        break;
                    case "InterfaceFITSThorIndex":
                        serviceProvider.GetService<InterfaceFITSThorIndex>().Run();
                        break;
                }

                consoleService.WriteLogs("---------- Program End ----------");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                System.Console.Error.Close();
                Environment.Exit(1);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private static void ConfigureServices(IServiceCollection services, string[] args)
        {
            //string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            //if (string.IsNullOrWhiteSpace(env))
            //{
            //    env = "Development";
            //}

            // build configuration
            var configuration = new ConfigurationBuilder()
              //.SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", true)
              .Build();


            services.AddHttpClient<ExternalInterfaceEndpoint>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(30);
                client.DefaultRequestHeaders.Add("x-access-token", configuration["TokenAPI"]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.BaseAddress = new Uri(configuration["ExternalInterfaceAPI"]);
            });

            services.AddHttpClient<StaticEndpoint>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(30);
                client.DefaultRequestHeaders.Add("x-access-token", configuration["TokenAPI"]);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.BaseAddress = new Uri(configuration["StaticAPI"]);
            });

            var webApi = services.AddTransient<WebAPI>();
            
            services.AddTransient<IConsoleService>(s => new ConsoleService(configuration, webApi.BuildServiceProvider().GetService<WebAPI>(), args));

            // Add Application
            services.AddTransient<ExportDWHIAS39ToSFTP>();
            services.AddTransient<ExportDMSDataSetToSFTP>();
            services.AddTransient<ExportDMSDataSetMonthlyToSFTP>();
            services.AddTransient<ExportFxReconcileToSFTP>();
            services.AddTransient<ExportGlToSFTP>();
            services.AddTransient<ExportFITSTrpToSFTP>();
            services.AddTransient<ExportAmendCancelDailyToEmail>();
            services.AddTransient<ExportAmendCancelMonthlyToEmail>();
            services.AddTransient<InterfaceFITSBondPledge>();
            services.AddTransient<InterfaceEQUITYPledge>();
            services.AddTransient<InterfaceCRTransLimit>();
            services.AddTransient<InterfaceCCM>();
            services.AddTransient<InterfaceBBGMarketPrice>();
            services.AddTransient<InterfaceMarketPriceTbmaFits>();
            services.AddTransient<InterfaceEQUITYNavPrice>();
            services.AddTransient<InterfaceSSMDExchangeRate>();
            services.AddTransient<InterfaceSSMDFloatingIndex>();
            services.AddTransient<InterfaceEXRATECounterRate>();
            services.AddTransient<InterfaceEXRATEMidValuationRate>();
            services.AddTransient<InternalBatchJobEod>();
            services.AddTransient<InternalBatchJobEndOfDay>();
            services.AddTransient<InternalBatchJobCheckEod>();
            services.AddTransient<InterfaceEQUITYPledge>();
            services.AddTransient<ExportUserProfileMonthlyToEmail>();
            services.AddTransient<ExportNesToSFTP>();
            services.AddTransient<InterfaceSSMDThorRate>();
            services.AddTransient<InterfaceFITSThorIndex>();
        }
    }
}
