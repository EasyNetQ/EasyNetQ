using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using EasyNetQ.SystemMessages;

namespace EasyNetQ.Scheduler
{
    public interface IScheduleRepository
    {
        void Store(ScheduleMe scheduleMe);
        IList<ScheduleMe> GetPending(DateTime timeNow);
    }

    public class ScheduleRepository : IScheduleRepository
    {
        private const string connectionStringKey = "scheduleDb";
        private readonly string connectionString;
        private const string insertSql = "uspAddNewMessageToScheduler";
        private const string selectSql = "uspGetNextBatchOfMessages";

        public ScheduleRepository()
        {
            connectionString = ConfigurationManager.ConnectionStrings[connectionStringKey].ConnectionString;
        }

        public ScheduleRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public void Store(ScheduleMe scheduleMe)
        {
            using(var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(insertSql, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@WakeTime", scheduleMe.WakeTime);
                command.Parameters.AddWithValue("@BindingKey", scheduleMe.BindingKey);
                command.Parameters.AddWithValue("@Message", scheduleMe.InnerMessage);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public IList<ScheduleMe> GetPending(DateTime timeNow)
        {
            var scheduledMessages = new List<ScheduleMe>();

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(selectSql, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@WakeTime", timeNow);

                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    scheduledMessages.Add(new ScheduleMe
                    {
                        WakeTime = reader.GetDateTime(2),
                        BindingKey = reader.GetString(3),
                        InnerMessage = reader.GetSqlBinary(4).Value
                    });
                }
            }

            return scheduledMessages;
        }
    }
}