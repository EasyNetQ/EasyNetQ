using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using EasyNetQ.SystemMessages;

namespace EasyNetQ.Scheduler
{
    public interface IScheduleRepository
    {
        void Store(ScheduleMe scheduleMe);
        void Cancel(UnscheduleMe unscheduleMe);
        IList<ScheduleMe> GetPending();
        void Purge();
    }

    public class ScheduleRepository : IScheduleRepository
    {
        private readonly ScheduleRepositoryConfiguration configuration;
        private readonly Func<DateTime> now;
        private readonly ISqlDialect dialect;

        public ScheduleRepository(ScheduleRepositoryConfiguration configuration, Func<DateTime> now)
        {
            this.configuration = configuration;
            this.now = now;
            this.dialect = SqlDialectResolver.Resolve(configuration.ProviderName);
        }

        public void Store(ScheduleMe scheduleMe)
        {
            WithStoredProcedureCommand(dialect.InsertProcedureName, command =>
            {
                AddParameter(command, dialect.WakeTimeParameterName, scheduleMe.WakeTime, DbType.DateTime);
                AddParameter(command, dialect.BindingKeyParameterName, scheduleMe.BindingKey, DbType.String);
                AddParameter(command, dialect.CancellationKeyParameterName, scheduleMe.CancellationKey, DbType.String);
                AddParameter(command, dialect.MessageParameterName, scheduleMe.InnerMessage, DbType.Binary);

                command.ExecuteNonQuery();
            });
        }

        public void Cancel(UnscheduleMe unscheduleMe)
        {
            ThreadPool.QueueUserWorkItem(state =>
                WithStoredProcedureCommand(dialect.CancelProcedureName, command =>
                {
                    AddParameter(command, dialect.CancellationKeyParameterName, unscheduleMe.CancellationKey, DbType.String);
    
                    command.ExecuteNonQuery();
                })
            );
        }

        public IList<ScheduleMe> GetPending()
        {
            var scheduledMessages = new List<ScheduleMe>();
            var scheduleMessageIds = new List<int>();

            WithStoredProcedureCommand(dialect.SelectProcedureName, command =>
            {
                AddParameter(command, dialect.RowsParameterName, configuration.MaximumScheduleMessagesToReturn, DbType.Int32);
                AddParameter(command, dialect.StatusParameterName, 0, DbType.Int16);
                AddParameter(command, dialect.WakeTimeParameterName, now(), DbType.DateTime);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        scheduledMessages.Add(new ScheduleMe
                        {
                            WakeTime = reader.GetDateTime(2),
                            BindingKey = reader.GetString(3),
                            InnerMessage = (byte[])reader.GetValue(4)
                        });

                        scheduleMessageIds.Add(reader.GetInt32(0));
                    }
                }
            });

            MarkItemsForPurge(scheduleMessageIds);

            return scheduledMessages;
        }

        public void MarkItemsForPurge(IEnumerable<int> scheduleMessageIds)
        {
            // mark items for purge on a background thread.
            ThreadPool.QueueUserWorkItem(state =>
                WithStoredProcedureCommand(dialect.MarkForPurgeProcedureName, command =>
                {
                    var purgeDate = now().AddDays(configuration.PurgeDelayDays);

                    var idParameter = AddParameter(command, dialect.IdParameterName, DbType.Int32);
                    AddParameter(command, dialect.PurgeDateParameterName, purgeDate, DbType.DateTime);

                    foreach (var scheduleMessageId in scheduleMessageIds)
                    {
                        idParameter.Value = scheduleMessageId;
                        command.ExecuteNonQuery();
                    }
                })
            );
        }

        public void Purge()
        {
            WithStoredProcedureCommand(dialect.PurgeProcedureName, command =>
                {
                    AddParameter(command, dialect.RowsParameterName, configuration.PurgeBatchSize, DbType.Int16);
                    AddParameter(command, dialect.PurgeDateParameterName, now(), DbType.DateTime);

                    command.ExecuteNonQuery();
                });
        }

        private void WithStoredProcedureCommand(string storedProcedureName, Action<IDbCommand> commandAction)
        {
            using (var connection = GetConnection())
            using (var command = CreateCommand(connection, FormatWithSchemaName(storedProcedureName)))
            {
                command.CommandType = CommandType.StoredProcedure;
                commandAction(command);
            }
        }

        private string FormatWithSchemaName(string storedProcedureName)
        {
            if (string.IsNullOrWhiteSpace(configuration.SchemaName))
                return storedProcedureName;

            return string.Format("[{0}].{1}", configuration.SchemaName.TrimStart('[').TrimEnd('.', ']'), storedProcedureName);
        }

        private IDbConnection GetConnection()
        {
            var factory = DbProviderFactories.GetFactory(configuration.ProviderName);
            var connection = factory.CreateConnection();
            connection.ConnectionString = configuration.ConnectionString;
            connection.Open();
            return connection;
        }

        private IDbCommand CreateCommand(IDbConnection connection, string commandText)
        {
            var command = connection.CreateCommand();
            command.CommandText = commandText;
            return command;
        }

        private void AddParameter(IDbCommand command, string parameterName, object value, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value;
            parameter.DbType = dbType;
            command.Parameters.Add(parameter);
        }

        private IDbDataParameter AddParameter(IDbCommand command, string parameterName, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            command.Parameters.Add(parameter);
            return parameter;
        }
    }
}