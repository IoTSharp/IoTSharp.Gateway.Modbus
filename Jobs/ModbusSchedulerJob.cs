﻿using IoTSharp.Gateways.Data;
using Quartz;
using Quartz.Impl;

namespace IoTSharp.Gateways.Jobs
{
    public class ModbusSchedulerJob : IJob
    {
        private ILogger _logger;

        private ApplicationDbContext _dbContext;
 
 
 

        public ModbusSchedulerJob(ILogger<ModbusSchedulerJob>  logger, ApplicationDbContext dbContext )
        {
            _logger = logger;
            _dbContext = dbContext;
        
        }

        public async Task Execute(IJobExecutionContext context)
        {
         var     _scheduler = context.Scheduler;
            var _modbuskeys = new JobKey(nameof(ModbusMasterJob));
            IJobDetail job;
            var jobexists = await _scheduler.CheckExists(_modbuskeys, context.CancellationToken);
            if (!jobexists)
            {

                job = JobBuilder.Create<ModbusMasterJob>()
                .WithIdentity(nameof( ModbusMasterJob))
                .StoreDurably()
                .Build();
              await  _scheduler.AddJob(job,true, context.CancellationToken);
            }
            else
            {
                job =await  _scheduler.GetJobDetail(_modbuskeys, context.CancellationToken);
            }
            var triggers = await _scheduler.GetTriggersOfJob(_modbuskeys, context.CancellationToken);
            var slaves = _dbContext.ModbusSlaves.ToList();
            var tgs = new List<ITrigger>();
            foreach (var slave in slaves)
            {

                var slaveid = slave.Id.ToString();
                int interval=  slave.TimeInterval==0 ? 30: (int)slave.TimeInterval;

                if (!triggers.Any(t => t.Key.Name == slaveid))
                {
                   var   trg = TriggerBuilder.Create()
                    .WithIdentity(slaveid)
                    .ForJob(job)
                    .UsingJobData("slave_id",slaveid)
                    .UsingJobData("slave_name", slave.DeviceName)
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(interval).RepeatForever()).StartNow()
                    .Build();
                    await _scheduler.ScheduleJob(trg,context.CancellationToken);
                }
            }
            _logger.LogInformation($"{_scheduler.IsStarted}");
        }
    }
}
