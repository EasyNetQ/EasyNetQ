using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace EasyNetQ
{
    public class SimpleTypeNameSerializer : ITypeNameSerializer
    {
        private readonly ConcurrentDictionary<string, Type> _deserializedTypes = new ConcurrentDictionary<string, Type>();
        private readonly ConcurrentDictionary<Type, string> _serializedTypes = new ConcurrentDictionary<Type, string>();

        public Type DeSerialize(string typeName)
        {
            return _deserializedTypes.GetOrAdd(typeName, SimpleTypeStringParser.Parse);
        }

        public string Serialize(Type type)
        {
            return _serializedTypes.GetOrAdd(type, t =>
            {
                var ns = String.Join(".", type.Namespace);
                var tn = type.Name;

                var typeName = new StringBuilder();
                typeName.Append(ns).Append(".").Append(tn);

                if (type.GenericTypeArguments.Length > 0)
                {
                    typeName.Append("[");
                    var needSep = false;
                    foreach (var genericTypeArgument in type.GenericTypeArguments)
                    {
                        if (needSep)
                        {
                            typeName.Append(", ");
                        }
                        typeName.Append("[");
                        typeName.Append(Serialize(genericTypeArgument));
                        typeName.Append("]");
                        needSep = true;
                    }
                    typeName.Append("]");
                }

                typeName.Append(", ").Append(type.Assembly.GetName().Name);

                var ret = typeName.ToString();
                if (typeName.Length > 255)
                {
                    throw new EasyNetQException("The serialized name of type '{0}' exceeds the AMQP " +
                                                "maximum short string length of 255 characters.", t.Name);
                }
                return ret;
            });
        }

        #region Type String Parser

        private class SimpleTypeStringParser
        {
            private enum Token
            {
                BracketLeft,
                BracketRight,
                Comma,
                String,
                EOT
            }

            private readonly string _typeString;
            private int _pos;
            private Token _token;
            private string _tokenValue;

            public SimpleTypeStringParser(string typeString)
            {
                _typeString = typeString;
                _pos = 0;
            }

            public static Type Parse(string typeString)
            {
                var parser = new SimpleTypeStringParser(typeString);
                return parser.Parse();
            }

            private Type Parse()
            {
                ReadNextToken();
                var ret = ReadType();
                if (_token != Token.EOT)
                {
                    throw new ArgumentException("invalid type string. unexpected input");
                }
                return ret;
            }

            private Type ReadType()
            {
                if (_token != Token.String)
                {
                    throw new ArgumentException("invalid type string. expecting type string.");
                }

                var typeName = _tokenValue;
                var typeArguments = new List<Type>();
                ReadNextToken();

                if (_token == Token.BracketLeft)
                {
                    ReadNextToken(); // read away [

                    do
                    {
                        if (_token == Token.Comma)
                        {
                            ReadNextToken(); // read away ,
                        }

                        if (_token != Token.BracketLeft)
                        {
                            throw new ArgumentException("invalid type string. expecting [");
                        }

                        ReadNextToken(); // read away [
                        typeArguments.Add(ReadType());

                        if (_token != Token.BracketRight)
                        {
                            throw new ArgumentException("invalid type string. expecting ]");
                        }
                        ReadNextToken(); // read away ]
                    } while (_token == Token.Comma);

                    if (_token != Token.BracketRight)
                    {
                        throw new ArgumentException("invalid type string. expecting ]");
                    }
                    ReadNextToken(); // read away ]
                }
                if (_token != Token.Comma)
                {
                    throw new ArgumentException("invalid type string. expecting ,");
                }
                ReadNextToken(); // read away ,
                if (_token != Token.String)
                {
                    throw new ArgumentException("invalid type string. expecting assembly string");
                }
                var assemblyName = _tokenValue;
                ReadNextToken(); // read away string
                var type = Type.GetType(typeName + ", " + assemblyName);
                if (typeArguments.Count > 0)
                {
                    type = type.MakeGenericType(typeArguments.ToArray());
                }
                return type;
            }

            private Token ReadNextToken()
            {
                _token = ReadNextTokenHelper();
                return _token;
            }

            private Token ReadNextTokenHelper()
            {
                if (_pos >= _typeString.Length)
                {
                    return Token.EOT;
                }

                while (Char.IsWhiteSpace(_typeString[_pos]))
                {
                    ++_pos;
                }

                var ch = _typeString[_pos];

                if (ch == '[')
                {
                    ++_pos;
                    return Token.BracketLeft;
                }
                if (ch == ']')
                {
                    ++_pos;
                    return Token.BracketRight;
                }
                if (ch == ',')
                {
                    ++_pos;
                    return Token.Comma;
                }

                _tokenValue = ReadString();
                return Token.String;
            }

            private string ReadString()
            {
                var ret = "";
                while (_pos < _typeString.Length)
                {
                    var ch = _typeString[_pos];
                    if (Char.IsLetterOrDigit(ch) ||
                        ch == '.' || ch == '`')
                    {
                        ret += ch;
                    }
                    else
                    {
                        break;
                    }
                    ++_pos;
                }

                return ret;
            }
        }

        #endregion Type String Parser
    }
}
