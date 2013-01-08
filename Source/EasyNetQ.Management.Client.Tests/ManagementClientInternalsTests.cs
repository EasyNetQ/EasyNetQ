using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyNetQ.Management.Client.Model;
using EasyNetQ.Management.Client.Serialization;
using NUnit.Framework;
using Newtonsoft.Json;

namespace EasyNetQ.Management.Client.Tests
{
    [TestFixture]
    public class ManagementClientInternalsTests
    {
        /// <summary>
        /// Checks for regression from using Int32 instead of Int64 for RecvOct and so on
        /// </summary>
        [Test]
        public void GetConnections_CheckDeserializeLargeNumbers()
        {
            //TODO: redesign the ManagementClient by factoring out some of it's responsibilities and use dependency injection
            //for this test we'd seperate out the deserialization.
            
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new RabbitContractResolver(),
            };

            settings.Converters.Add(new PropertyConverter());

            String responseBody = GetExampleGetConnectionsJsonResponseBody();

            var connections = JsonConvert.DeserializeObject<IEnumerable<Connection>>(responseBody, settings);
        }

        private String GetExampleGetConnectionsJsonResponseBody()
        {
            return @"
[{
	""recv_oct"": 2899479294,
	""recv_cnt"": 7765146,
	""send_oct"": 2956033026,
	""send_cnt"": 11310800,
	""send_pend"": 0,
	""state"": ""running"",
	""channels"": 2,
	""recv_oct_details"": {
		""rate"": 657003.9141596435,
		""interval"": 5000818,
		""last_event"": 1357575675448
	},
	""send_oct_details"": {
		""rate"": 669916.001742115,
		""interval"": 5000818,
		""last_event"": 1357575675448
	},
	""name"": ""10.1.1.55:50703"",
	""type"": ""network"",
	""node"": ""rabbit@centosRabbit"",
	""address"": ""10.1.1.67"",
	""port"": 5672,
	""peer_address"": ""10.1.1.55"",
	""peer_port"": 50703,
	""ssl"": false,
	""peer_cert_subject"": """",
	""peer_cert_issuer"": """",
	""peer_cert_validity"": """",
	""auth_mechanism"": ""PLAIN"",
	""ssl_protocol"": """",
	""ssl_key_exchange"": """",
	""ssl_cipher"": """",
	""ssl_hash"": """",
	""protocol"": ""AMQP 0-9-1"",
	""user"": ""liquid_dialler_user"",
	""vhost"": ""DANDESKTOP"",
	""timeout"": 0,
	""frame_max"": 131072,
	""client_properties"": {
		""x-Liquid-MachineName"": ""DANDESKTOP"",
		""platform"": "".NET"",
		""copyright"": ""Copyright (C) 2007-2012 VMware, Inc."",
		""version"": ""3.0.0.0"",
		""information"": ""Licensed under the MPL.  See http://www.rabbitmq.com/"",
		""capabilities"": {
			""publisher_confirms"": true,
			""exchange_exchange_bindings"": true,
			""consumer_cancel_notify"": true,
			""basic.nack"": true
		},
		""product"": ""RabbitMQ"",
		""x-Liquid-Process"": ""Some useful description""
	}
}]";
        }
    }
}
