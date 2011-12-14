using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using EasyNetQ.SystemMessages;

namespace EasyNetQ.Scheduler
{
    public interface IScheduleRepository
    {
        void Store(ScheduleMe scheduleMe);
        IList<ScheduleMe> GetPending();
        void Purge();
    }

    public class ScheduleRepository : IScheduleRepository
    {
        private const string insertSql = "uspAddNewMessageToScheduler";
        private const string selectSql = "uspGetNextBatchOfMessages";
        private const string purgeSql = "uspWorkItemsSelfPurge";
        private const string markForPurgeSql = "uspMarkWorkItemForPurge";

        private readonly ScheduleRepositoryConfiguration configuration;
        private readonly Func<DateTime> now; 

        public ScheduleRepository(ScheduleRepositoryConfiguration configuration, Func<DateTime> now)
        {
            this.configuration = configuration;
            this.now = now;
        }

        public void Store(ScheduleMe scheduleMe)
        {
            WithStoredProcedureCommand(insertSql, command =>
            {
                command.Parameters.AddWithValue("@WakeTime", scheduleMe.WakeTime);
                command.Parameters.AddWithValue("@BindingKey", scheduleMe.BindingKey);
                command.Parameters.AddWithValue("@Message", scheduleMe.InnerMessage);

                command.ExecuteNonQuery();
            });
        }

        public IList<ScheduleMe> GetPending()
        {
            var scheduledMessages = new List<ScheduleMe>();
            var scheuldeMessageIds = new List<int>();

            WithStoredProcedureCommand(selectSql, command =>
            {
                command.Parameters.AddWithValue("@WakeTime", now());
                command.Parameters.AddWithValue("@rows", configuration.MaximumScheduleMessagesToReturn);

                using(var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        scheduledMessages.Add(new ScheduleMe
                        {
                            WakeTime = reader.GetDateTime(2),
                            BindingKey = reader.GetString(3),
                            InnerMessage = reader.GetSqlBinary(4).Value
                        });

                        scheuldeMessageIds.Add(reader.GetInt32(0));
                    }
                }
            });

            MarkItemsForPurge(scheuldeMessageIds);

            return scheduledMessages;
        }

        public void MarkItemsForPurge(IEnumerable<int> scheuldeMessageIds)
        {
            // mark items for purge on a background thread.
            ThreadPool.QueueUserWorkItem(state => 
                WithStoredProcedureCommand(markForPurgeSql, command =>
                {
                    var purgeDate = now().AddDays(configuration.PurgeDelayDays);

                    command.Parameters.AddWithValue("@purgeDate", purgeDate);
                    var idParameter = command.Parameters.Add("@ID", SqlDbType.Int);

                    foreach (var scheduleMessageId in scheuldeMessageIds)
                    {
                        idParameter.Value = scheduleMessageId;
                        command.ExecuteNonQuery();
                    }
                })
            );
        }

        public void Purge()
        {
            WithStoredProcedureCommand(purgeSql, command =>
            {
                command.Parameters.AddWithValue("@rows", configuration.PurgeBatchSize);

                command.ExecuteNonQuery();
            });
        }

        private void WithStoredProcedureCommand(string storedProcedureName, Action<SqlCommand> commandAction)
        {
            using (var connection = new SqlConnection(configuration.ConnectionString))
            using (var command = new SqlCommand(storedProcedureName, connection))
            {
                connection.Open();
                command.CommandType = CommandType.StoredProcedure;

                commandAction(command);
            }
        }
    }
}