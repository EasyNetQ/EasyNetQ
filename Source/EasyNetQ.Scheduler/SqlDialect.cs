using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Scheduler
{
    public interface ISqlDialect
    {
        bool IsDialectFor(string providerName);
        string InsertProcedureName { get; }
        string CancelProcedureName { get; }
        string SelectProcedureName { get; }
        string PurgeProcedureName { get; }
        string MarkForPurgeProcedureName { get; }
        string WakeTimeParameterName { get; }
        string BindingKeyParameterName { get; }
        string CancellationKeyParameterName { get; }
        string MessageParameterName { get; }
        string PurgeDateParameterName { get; }
        string RowsParameterName { get; }
        string IdParameterName { get; }
        string StatusParameterName { get; }
        string ExchangeParameterName { get; }
        string ExchangeTypeParameterName { get; }
        string RoutingKeyParameterName { get; }
        string MessagePropertiesParameterName { get; }
        string InstanceNameParameterName { get; }
    }

    public class SqlDialectResolver
    {
        private static readonly List<ISqlDialect> dialects = new List<ISqlDialect>();

        static SqlDialectResolver()
        {
            dialects.Add(new MssqlDialect());
            dialects.Add(new PostgreSqlDialect());
        }

        public static ISqlDialect Resolve(string providerName)
        {
            var dialect = dialects.First(x => x.IsDialectFor(providerName));
            return dialect;
        }
    }

    public class MssqlDialect : ISqlDialect
    {
        public MssqlDialect()
        {
            InsertProcedureName = "uspAddNewMessageToScheduler";
            CancelProcedureName = "uspCancelScheduledMessages";
            SelectProcedureName = "uspGetNextBatchOfMessages";
            PurgeProcedureName = "uspWorkItemsSelfPurge";
            MarkForPurgeProcedureName = "uspMarkWorkItemForPurge";
            WakeTimeParameterName = "@WakeTime";
            BindingKeyParameterName = "@BindingKey";
            CancellationKeyParameterName = "@CancellationKey";
            MessageParameterName = "@Message";
            PurgeDateParameterName = "@purgeDate";
            RowsParameterName = "@rows";
            IdParameterName = "@ID";
            StatusParameterName = "@status";
            InstanceNameParameterName = "@InstanceName";
            ExchangeParameterName = "@Exchange";
            ExchangeTypeParameterName = "@ExchangeType";
            RoutingKeyParameterName = "@RoutingKey";
            MessagePropertiesParameterName = "@MessageProperties";
        }

        public bool IsDialectFor(string providerName)
        {
            return providerName == "System.Data.SqlClient";
        }

        public string InsertProcedureName { get; private set; }
        public string CancelProcedureName { get; private set; }
        public string SelectProcedureName { get; private set; }
        public string PurgeProcedureName { get; private set; }
        public string MarkForPurgeProcedureName { get; private set; }
        public string WakeTimeParameterName { get; private set; }
        public string BindingKeyParameterName { get; private set; }
        public string CancellationKeyParameterName { get; private set; }
        public string MessageParameterName { get; private set; }
        public string PurgeDateParameterName { get; private set; }
        public string RowsParameterName { get; private set; }
        public string IdParameterName { get; private set; }
        public string StatusParameterName { get; private set; }
        public string ExchangeParameterName { get; private set; }
        public string ExchangeTypeParameterName { get; private set; }
        public string RoutingKeyParameterName { get; private set; }
        public string MessagePropertiesParameterName { get; private set; }
        public string InstanceNameParameterName { get; private set; }
    }

    public class PostgreSqlDialect : ISqlDialect
    {
        public PostgreSqlDialect()
        {
            InsertProcedureName = "\"uspAddNewMessageToScheduler\"";
            CancelProcedureName = "\"uspCancelScheduledMessages\"";
            SelectProcedureName = "\"uspGetNextBatchOfMessages\"";
            PurgeProcedureName = "\"uspWorkItemsSelfPurge\"";
            MarkForPurgeProcedureName = "\"uspMarkWorkItemForPurge\"";
            WakeTimeParameterName = "p_wakeTime";
            BindingKeyParameterName = "p_bindingKey";
            CancellationKeyParameterName = "p_cancellationKey";
            MessageParameterName = "p_message";
            PurgeDateParameterName = "p_purgeDate";
            RowsParameterName = "p_rows";
            IdParameterName = "p_id";
            StatusParameterName = "p_status";
            InstanceNameParameterName = "p_instanceName";
            ExchangeParameterName = "p_exchange";
            ExchangeTypeParameterName = "p_exchangeType";
            RoutingKeyParameterName = "p_routingKey";
            MessagePropertiesParameterName = "p_messageProperties";
        }

        public bool IsDialectFor(string providerName)
        {
            return
				providerName == "Npgsql" ||
				providerName == "Devart.Data.PostgreSql";
        }

        public string InsertProcedureName { get; private set; }
        public string CancelProcedureName { get; private set; }
        public string SelectProcedureName { get; private set; }
        public string PurgeProcedureName { get; private set; }
        public string MarkForPurgeProcedureName { get; private set; }
        public string WakeTimeParameterName { get; private set; }
        public string BindingKeyParameterName { get; private set; }
        public string CancellationKeyParameterName { get; private set; }
        public string MessageParameterName { get; private set; }
        public string PurgeDateParameterName { get; private set; }
        public string RowsParameterName { get; private set; }
        public string IdParameterName { get; private set; }
        public string StatusParameterName { get; private set; }
        public string InstanceNameParameterName { get; private set; }
        public string ExchangeParameterName { get; private set; }
        public string ExchangeTypeParameterName { get; private set; }
        public string RoutingKeyParameterName { get; private set; }
        public string MessagePropertiesParameterName { get; private set; }
    }
}