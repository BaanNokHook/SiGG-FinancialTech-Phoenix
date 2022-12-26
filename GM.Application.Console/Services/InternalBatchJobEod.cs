using System;
using System.Net.Http;
using GM.Application.Console.Model;
using GM.ClientAPI;
using GM.CommonLibs.Common;
using GM.Model.ExternalInterface;

namespace GM.Application.Console.Services
{
    public class InternalBatchJobEod : WebAPI
    {
        private DateTime systemDate;
        private IConsoleService _service;
        private static Console_Entity ConsoleEnt = new Console_Entity();
        private static Mail_AdminEntity MailAdminEnt = new Mail_AdminEntity();

        public InternalBatchJobEod(IHttpClientFactory httpClientFactory, IConsoleService consoleService) : base(httpClientFactory)
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
                //Step 1 : Get Config
                string inputDate = _service.GetInputDate();
                systemDate = _service.GetBusinessDateOrSystemDate(inputDate);

                InternalJobModel InternalJobModel = new InternalJobModel();
                InternalJobModel.AsofDate = systemDate.ToString("dd/MM/yyyy");

                var ResultInternalJob = ExternalInterfaceAPI.InternalJob.InternalBatchJobEod(InternalJobModel);
                if (!ResultInternalJob.Success)
                {
                    throw new Exception("InternalBatchJobEod() => [" + ResultInternalJob.RefCode + "] " + ResultInternalJob.Message);
                }

                _service.WriteLogs("ReturnCode = [" + ResultInternalJob.RefCode + "] " + ResultInternalJob.Message);
                _service.WriteLogs("InternalBatchJobEod Success.");

            }
            catch (Exception Ex)
            {
                _service.WriteLogs("Error " + Ex.Message);
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
