using System;
using System.Collections.Generic;
using System.Configuration;
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
        private const string insertSql = 
@"
insert into ScheduleMe (WakeTime, BindingKey, InnerMessage, IsComplete) 
    values (@WakeTime, @BindingKey, @InnerMessage, 0)
";
        private const string selectSql =
@"
select WakeTime, BindingKey, InnerMessage from ScheduleMe where WakeTime <= @TimeNow and IsComplete = 0;
update ScheduleMe set IsComplete = 1 where WakeTime <= @TimeNow and IsComplete = 0;
";

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
                command.Parameters.AddWithValue("@WakeTime", scheduleMe.WakeTime);
                command.Parameters.AddWithValue("@BindingKey", scheduleMe.BindingKey);
                command.Parameters.AddWithValue("@InnerMessage", scheduleMe.InnerMessage);

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
                command.Parameters.AddWithValue("@TimeNow", timeNow);

                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    scheduledMessages.Add(new ScheduleMe
                    {
                        WakeTime = reader.GetDateTime(0),
                        BindingKey = reader.GetString(1),
                        InnerMessage = reader.GetSqlBinary(2).Value
                    });
                }
            }

            return scheduledMessages;
        }
    }
}