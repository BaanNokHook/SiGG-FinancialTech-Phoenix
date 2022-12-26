using System;
using System.Net.Http;
using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.ExternalInterface;

namespace GM.Application.Console.Services
{
    public class InternalBatchJobEndOfDay : WebAPI
    {
        private DateTime systemDate;
        private IConsoleService _service;
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();

        public InternalBatchJobEndOfDay(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
        {
            _service = consoleService;
        }

        public bool Run()
        {
            string StrMsg = string.Empty;
            bool SendEmail = false;
            try
            {
                _service.WriteLogs("### START RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");

                InternalJobModel InternalJobModel = new InternalJobModel();
                var ResultInternalJob = ExternalInterfaceAPI.InternalJob.InternalBatchJobEndOfDay(InternalJobModel);
                if (!ResultInternalJob.Success)
                {
                    throw new Exception("InternalBatchJobEndOfDay() => [" + ResultInternalJob.RefCode + "] " + ResultInternalJob.Message);
                }

                _service.WriteLogs("ReturnCode = [" + ResultInternalJob.RefCode + "] " + ResultInternalJob.Message);
                _service.WriteLogs("InternalBatchJobEod Success.");

            }
            catch (Exception ex)
            {
                _service.WriteLogs("Error " + ex.Message);
                SendEmail = true;
            }
            finally
            {
                _service.WriteLogs("### END RUN FUNCTION [" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "] : [" + _service.GetFunction() + "()] ###");
                if (SendEmail)
                {
                    _service.SendMailError(MailAdminEnt);
                }
            }
            return true;
        }
    }
}
