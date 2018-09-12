﻿using AutoMapper;
using Hangfire;
using System.Linq;
using System.Web.Mvc;
using WorkFlowManager.Common.DataAccess._UnitOfWork;
using WorkFlowManager.Common.Tables;
using WorkFlowManager.Common.ViewModels;

namespace WorkFlowManager.Services.DbServices
{
    public class TestWorkFlowProcessService : WorkFlowProcessService, IWorkFlow
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly WorkFlowDataService _workFlowDataService;

        public TestWorkFlowProcessService(
                IUnitOfWork unitOfWork, WorkFlowDataService workFlowDataService)
                : base(unitOfWork, workFlowDataService)
        {
            _unitOfWork = unitOfWork;
            _workFlowDataService = workFlowDataService;
        }


        #region Decission Methods
        public char ReturnYes()
        {
            return 'Y';
        }

        public char ReturnNo()
        {
            return 'N';
        }

        private int GetOwnerIdFromId(int id)
        {
            var workFlowTrace = _unitOfWork.Repository<WorkFlowTrace>().Get(x => x.Id == id);
            int rslt = -1;
            if (workFlowTrace != null)
            {
                rslt = workFlowTrace.OwnerId;
            }
            return rslt;
        }

        private int GetSelectedConditionOptionIdForCondition(string conditionName, int ownerId)
        {
            var condition = _unitOfWork.Repository<Condition>().GetAll().Where(x => x.Name == conditionName).FirstOrDefault();
            var option = _unitOfWork.Repository<WorkFlowTrace>().GetAll().Where(x => x.OwnerId == ownerId && x.ProcessId == condition.Id).OrderByDescending(x => x.Id).FirstOrDefault();
            return (int)option.ConditionOptionId;
        }

        public char IsAgeLessThan(string id, string age)
        {
            var rslt = 'N';
            int ownerId = GetOwnerIdFromId(int.Parse(id));
            var testForm = _unitOfWork.Repository<TestForm>().Get(x => x.OwnerId == ownerId);
            if (testForm != null)
            {
                if (testForm.Age < int.Parse(age))
                {
                    rslt = 'Y';
                }
            }
            return rslt;
        }


        public char IsAgeGreaterThan(string id, string age)
        {
            var rslt = 'N';
            int ownerId = GetOwnerIdFromId(int.Parse(id));
            var testForm = _unitOfWork.Repository<TestForm>().Get(x => x.OwnerId == ownerId);
            if (testForm != null)
            {
                if (testForm.Age > int.Parse(age))
                {
                    rslt = 'Y';
                }
                else
                {
                    testForm.Age = testForm.Age + 1;
                    _unitOfWork.Repository<TestForm>().Update(testForm);
                    _unitOfWork.Complete();
                }
            }
            return rslt;
        }

        public char IsCandidateColorBlind(string id)
        {
            var rslt = 'N';
            int ownerId = GetOwnerIdFromId(int.Parse(id));
            var conditionOptionId = GetSelectedConditionOptionIdForCondition("Select Eye Condition", ownerId);

            var conditionOption = _unitOfWork.Repository<ConditionOption>().Get(x => x.Id == conditionOptionId);
            if (conditionOption.Name == "Color-Blind")
            {
                rslt = 'Y';
            }
            return rslt;
        }


        #endregion

        #region Workflow
        public int StartWorkFlow(int ownerId, int taskId)
        {

            var task = _unitOfWork.Repository<Task>().Get(taskId);

            WorkFlowTrace workFlowTrace = null;

            workFlowTrace = new WorkFlowTrace()
            {
                ProcessId = (int)task.StartingProcessId,
                OwnerId = ownerId,
                ProcessStatus = Common.Enums.ProcessStatus.Draft
            };
            AddOrUpdate(workFlowTrace);
            return workFlowTrace.Id;
        }


        public bool FormValidate(WorkFlowFormViewModel formData, ModelStateDictionary modelState)
        {
            return base.CustomFormValidate(formData, modelState);
        }


        public override void FormSave(WorkFlowFormViewModel formData)
        {
            base.CustomFormSave(formData);
        }

        public override void WorkFlowFormSave<TClass, TVM>(WorkFlowFormViewModel workFlowFormViewModel)
        {
            base.WorkFlowFormSave<TClass, TVM>(workFlowFormViewModel);
            WorkFlowTrace torSatinAlmaIslem = Mapper.Map<WorkFlowFormViewModel, WorkFlowTrace>(workFlowFormViewModel);
            AddOrUpdate(torSatinAlmaIslem);
        }

        public override void WorkFlowProcessCancel(int workFlowTraceId)
        {
            base.WorkFlowProcessCancel(workFlowTraceId);
        }

        public override void CancelWorkFlowTrace(int workFlowTraceId, int targetProcessId)
        {
            base.CancelWorkFlowTrace(workFlowTraceId, targetProcessId);
        }

        public override void WorkFlowWorkFlowNextProcess(int ownerId)
        {
            base.WorkFlowWorkFlowNextProcess(ownerId);
        }

        public string DecisionPointJobCall(string id, string jobId, string hourInterval)
        {
            base.DecisionPointJobCallBase(id, jobId, hourInterval);

            RecurringJob.AddOrUpdate<TestWorkFlowProcessService>(jobId, x => x.DecisionPointTakeTheNextStep(int.Parse(id)), Cron.HourInterval(int.Parse(hourInterval)));
            return "OK";
        }
        #endregion

    }
}
