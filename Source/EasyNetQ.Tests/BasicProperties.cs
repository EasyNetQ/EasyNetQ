using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Impl;

namespace EasyNetQ.Tests
{
 public sealed class BasicProperties : IBasicProperties
  {
    private string _contentType;
    private string _contentEncoding;
    private IDictionary<string, object> _headers;
    private byte _deliveryMode;
    private byte _priority;
    private string _correlationId;
    private string _replyTo;
    private string _expiration;
    private string _messageId;
    private AmqpTimestamp _timestamp;
    private string _type;
    private string _userId;
    private string _appId;
    private string _clusterId;

    private bool _contentType_present = false;
    private bool _contentEncoding_present = false;
    private bool _headers_present = false;
    private bool _deliveryMode_present = false;
    private bool _priority_present = false;
    private bool _correlationId_present = false;
    private bool _replyTo_present = false;
    private bool _expiration_present = false;
    private bool _messageId_present = false;
    private bool _timestamp_present = false;
    private bool _type_present = false;
    private bool _userId_present = false;
    private bool _appId_present = false;
    private bool _clusterId_present = false;

    public string ContentType
    {
      get => _contentType;
      set
      {
        _contentType_present = value != null;
        _contentType = value;
      }
    }

    public string ContentEncoding
    {
      get => _contentEncoding;
      set
      {
        _contentEncoding_present = value != null;
        _contentEncoding = value;
      }
    }

    public IDictionary<string, object> Headers
    {
      get => _headers;
      set
      {
        _headers_present = value != null;
        _headers = value;
      }
    }

    public byte DeliveryMode
    {
      get => _deliveryMode;
      set
      {
        _deliveryMode_present = true;
        _deliveryMode = value;
      }
    }

    public bool Persistent
    {
        get { return DeliveryMode == 2; }
        set { DeliveryMode = value ? (byte)2 : (byte)1; }
    }

    public byte Priority
    {
      get => _priority;
      set
      {
        _priority_present = true;
        _priority = value;
      }
    }

    public string CorrelationId
    {
      get => _correlationId;
      set
      {
        _correlationId_present = value != null;
        _correlationId = value;
      }
    }

    public string ReplyTo
    {
      get => _replyTo;
      set
      {
        _replyTo_present = value != null;
        _replyTo = value;
      }
    }

    public string Expiration
    {
      get => _expiration;
      set
      {
        _expiration_present = value != null;
        _expiration = value;
      }
    }

    public string MessageId
    {
      get => _messageId;
      set
      {
        _messageId_present = value != null;
        _messageId = value;
      }
    }

    public AmqpTimestamp Timestamp
    {
      get => _timestamp;
      set
      {
        _timestamp_present = true;
        _timestamp = value;
      }
    }

    public string Type
    {
      get => _type;
      set
      {
        _type_present = value != null;
        _type = value;
      }
    }

    public string UserId
    {
      get => _userId;
      set
      {
        _userId_present = value != null;
        _userId = value;
      }
    }

    public string AppId
    {
      get => _appId;
      set
      {
        _appId_present = value != null;
        _appId = value;
      }
    }

    public string ClusterId
    {
      get => _clusterId;
      set
      {
        _clusterId_present = value != null;
        _clusterId = value;
      }
    }

    public void ClearContentType() => _contentType_present = false;

    public void ClearContentEncoding() => _contentEncoding_present = false;

    public void ClearHeaders() => _headers_present = false;

    public void ClearDeliveryMode() => _deliveryMode_present = false;

    public void ClearPriority() => _priority_present = false;

    public void ClearCorrelationId() => _correlationId_present = false;

    public void ClearReplyTo() => _replyTo_present = false;

    public void ClearExpiration() => _expiration_present = false;

    public void ClearMessageId() => _messageId_present = false;

    public void ClearTimestamp() => _timestamp_present = false;

    public void ClearType() => _type_present = false;

    public void ClearUserId() => _userId_present = false;

    public void ClearAppId() => _appId_present = false;

    public void ClearClusterId() => _clusterId_present = false;

    public bool IsContentTypePresent() => _contentType_present;

    public bool IsContentEncodingPresent() => _contentEncoding_present;

    public bool IsHeadersPresent() => _headers_present;

    public bool IsDeliveryModePresent() => _deliveryMode_present;

    public bool IsPriorityPresent() => _priority_present;

    public bool IsCorrelationIdPresent() => _correlationId_present;

    public bool IsReplyToPresent() => _replyTo_present;

    public bool IsExpirationPresent() => _expiration_present;

    public bool IsMessageIdPresent() => _messageId_present;

    public bool IsTimestampPresent() => _timestamp_present;

    public bool IsTypePresent() => _type_present;

    public bool IsUserIdPresent() => _userId_present;

    public bool IsAppIdPresent() => _appId_present;

    public bool IsClusterIdPresent() => _clusterId_present;

    public PublicationAddress ReplyToAddress
    {
        get { return PublicationAddress.Parse(ReplyTo); }
        set { ReplyTo = value.ToString(); }
    }

    public BasicProperties() { }
    public ushort ProtocolClassId => 60;
    public string ProtocolClassName => "basic";

    public void AppendPropertyDebugStringTo(StringBuilder sb)
    {
      sb.Append("(");
      sb.Append("content-type="); sb.Append(_contentType_present ? (_contentType == null ? "(null)" : _contentType.ToString()) : "_"); sb.Append(", ");
      sb.Append("content-encoding="); sb.Append(_contentEncoding_present ? (_contentEncoding == null ? "(null)" : _contentEncoding.ToString()) : "_"); sb.Append(", ");
      sb.Append("headers="); sb.Append(_headers_present ? (_headers == null ? "(null)" : _headers.ToString()) : "_"); sb.Append(", ");
      sb.Append("delivery-mode="); sb.Append(_deliveryMode_present ? _deliveryMode.ToString() : "_"); sb.Append(", ");
      sb.Append("priority="); sb.Append(_priority_present ? _priority.ToString() : "_"); sb.Append(", ");
      sb.Append("correlation-id="); sb.Append(_correlationId_present ? (_correlationId == null ? "(null)" : _correlationId.ToString()) : "_"); sb.Append(", ");
      sb.Append("reply-to="); sb.Append(_replyTo_present ? (_replyTo == null ? "(null)" : _replyTo.ToString()) : "_"); sb.Append(", ");
      sb.Append("expiration="); sb.Append(_expiration_present ? (_expiration == null ? "(null)" : _expiration.ToString()) : "_"); sb.Append(", ");
      sb.Append("message-id="); sb.Append(_messageId_present ? (_messageId == null ? "(null)" : _messageId.ToString()) : "_"); sb.Append(", ");
      sb.Append("timestamp="); sb.Append(_timestamp_present ? _timestamp.ToString() : "_"); sb.Append(", ");
      sb.Append("type="); sb.Append(_type_present ? (_type == null ? "(null)" : _type.ToString()) : "_"); sb.Append(", ");
      sb.Append("user-id="); sb.Append(_userId_present ? (_userId == null ? "(null)" : _userId.ToString()) : "_"); sb.Append(", ");
      sb.Append("app-id="); sb.Append(_appId_present ? (_appId == null ? "(null)" : _appId.ToString()) : "_"); sb.Append(", ");
      sb.Append("cluster-id="); sb.Append(_clusterId_present ? (_clusterId == null ? "(null)" : _clusterId.ToString()) : "_");
      sb.Append(")");
    }
  }
}
