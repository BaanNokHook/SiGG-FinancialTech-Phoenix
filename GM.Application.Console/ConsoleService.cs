using System;
using System.Globalization;
using System.Threading;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.Common;
using GM.Model.ExternalInterface;
using GM.Model.Static;
using Microsoft.Extensions.Configuration;

namespace GM.Application.Console
{
    public interface IConsoleService
    {
        string GetCommandLine(string cmdStr);
        string GetParameter(string[] args);
        string GetFunction();
        string GetInputDate();
        DateTime GetBusinessDateOrSystemDate(string inputDate);
        bool CheckHoliday(DateTime inputDate);
        void UpdateCheckingEod();
        void WriteLogs(string msg);
        void SendMailError(Mail_AdminEntity mailAdminEntity);
    }

    public class ConsoleService : IConsoleService
    {
        private readonly LogFile _log;
        private readonly string _logFilePath;
        private readonly string _function;
        private readonly string _inputDate;
        private readonly WebAPI _webApi;
        private string _errorMsg;

        public ConsoleService(IConfiguration configuration, WebAPI webApi, string[] args)
        {
            _log = new LogFile(configuration);
            _logFilePath = configuration["LogFilePath"];
            _function = GetCommandLine(args[0]);
            _inputDate = GetParameter(args);
            _webApi = webApi;
        }

        #region Function : Main
        public string GetCommandLine(string cmdStr)
        {
            try
            {
                string[] cmdArray_rec = cmdStr.Split('-');
                string[] paramsArray_rec = null;

                if (cmdArray_rec.Length > 0)
                {
                    paramsArray_rec = cmdArray_rec[1].Split(':');
                }

                if (paramsArray_rec != null && paramsArray_rec[0] == "f")
                {
                    return paramsArray_rec[1];
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public string GetFunction()
        {
            return _function;
        }

        public string GetInputDate()
        {
            return _inputDate;
        }

        public string GetParameter(string[] args)
        {
            try
            {
                if (args.Length > 1)
                {
                    string cmdStr = args[1];
                    string[] cmdArray_rec = cmdStr.Split('-');
                    if (cmdArray_rec != null)
                    {
                        return cmdArray_rec[1];
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return string.Empty;
        }

        public void UpdateCheckingEod()
        {
            _webApi.ExternalInterfaceAPI.InterfaceCheckingEod.UpdateCheckingEod(new InterfaceCheckingEodModel() { task_name = _function });
        }

        #endregion

        public DateTime GetBusinessDateOrSystemDate(string inputDate)
        {
            DateTime systemDate;
            try
            {
                if (inputDate == string.Empty)
                {
                    RpDateModel RpDateModel = new RpDateModel();

                    //Add Paging
                    PagingModel paging = new PagingModel();
                    paging.PageNumber = 1;
                    paging.RecordPerPage = 20;
                    RpDateModel.paging = paging;

                    var Result = _webApi.StaticAPI.BusinessDate.GetBusinessDateOrSystemDate(RpDateModel);

                    if (!Result.Success)
                    {
                        throw new Exception("[" + Result.RefCode + "] " + Result.Message);
                    }

                    systemDate = Result.Data[0].RpDate;
                    var type = Result.Data[0].Type;

                    WriteLogs("GetDate          : " + systemDate.ToString("yyyyMMdd"));
                    WriteLogs("TypeDate         : " + type);
                }
                else
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                    if (inputDate.Trim() != string.Empty)
                    {
                        systemDate = DateTime.ParseExact(inputDate, "yyyyMMdd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        systemDate = DateTime.Now.Date;
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("GetBusinessDateOrSystemDate() => " + ex.Message);
            }

            return systemDate;
        }

        public bool CheckHoliday(DateTime inputDate)
        {
            try
            {
                RpHolidayModel RpHolidayModel = new RpHolidayModel();

                //Add Paging
                PagingModel paging = new PagingModel();
                paging.PageNumber = 1;
                paging.RecordPerPage = 20;
                RpHolidayModel.paging = paging;
                RpHolidayModel.check_date = inputDate;
                RpHolidayModel.cur = "THB";

                var Result = _webApi.StaticAPI.Holiday.CheckHoliday(RpHolidayModel);
                if (!Result.Success)
                {
                    throw new Exception("[" + Result.RefCode + "] " + Result.Message);
                }

                return Result.Data[0].is_holiday;
            }
            catch (Exception ex)
            {
                throw new Exception("Check_Holiday() " + ex.Message);
            }
        }

        public void SendMailError(Mail_AdminEntity mailAdminEntity)
        {
            SendMail ObjMail = new SendMail();
            if (mailAdminEntity.To.Count > 0)
            {
                for (int i = 0; i < mailAdminEntity.To.Count; i++)
                {
                    WriteLogs("-> Send Mail Error To : " + mailAdminEntity.To[i]);
                }

                ObjMail.SendMailAdmin(mailAdminEntity, _errorMsg);

            }
        }

        public void WriteLogs(string msg)
        {
            _errorMsg += msg + "<br>";
            _log.WriteLogConsole(msg, _logFilePath, _function);
        }
    }
}
