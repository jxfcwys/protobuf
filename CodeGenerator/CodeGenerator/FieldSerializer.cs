using System;
using System.Collections.Generic;
using SilentOrbit.Code;

namespace SilentOrbit.ProtocolBuffers
{
    class FieldSerializer
    {
        readonly CodeWriter cw;
        readonly Options options;

        public FieldSerializer(CodeWriter cw, Options options)
        {
            this.cw = cw;
            this.options = options;
        }

        #region Reader

        /// <summary>
        /// Return true for normal code and false if generated thrown exception.
        /// In the latter case a break is not needed to be generated afterwards.
        /// </summary>
        public bool FieldReader(Field f)
        {
            if (f.Rule == FieldRule.Repeated)
            {
                //Make sure we are not reading a list of interfaces
                cw.Comment("repeated");
                cw.WriteLine("instance." + f.CsName + ".Add(" + FieldReaderType(f, "stream", "br", null) + ");");
            }
            else
            {
                if (f.ProtoType is ProtoMessage)
                {
                    cw.WriteLine("if (instance." + f.CsName + " == null)");
                    cw.WriteIndent("instance." + f.CsName + " = " + FieldReaderType(f, "stream", "br", null) + ";");
                    cw.WriteLine("else");
                    cw.WriteIndent(FieldReaderType(f, "stream", "br", "instance." + f.CsName) + ";");
                    return true;
                }

                cw.WriteLine("instance." + f.CsName + " = " + FieldReaderType(f, "stream", "br", "instance." + f.CsName) + ";");
            }
            return true;
        }

        /// <summary>
        /// Read a primitive from the stream
        /// </summary>
        string FieldReaderType(Field f, string stream, string binaryReader, string instance)
        {
            return FieldReaderPrimitive(f, stream, binaryReader, instance);
        }

        static string FieldReaderPrimitive(Field f, string stream, string binaryReader, string instance)
        {
            if (f.ProtoType is ProtoMessage)
            {
                var m = f.ProtoType as ProtoMessage;
                if (f.Rule == FieldRule.Repeated || instance == null)
                    return m.FullSerializerType + ".DeserializeLengthDelimited(" + stream + ")";
                else
                    return m.FullSerializerType + ".DeserializeLengthDelimited(" + stream + ", " + instance + ")";
            }

            if (f.ProtoType is ProtoEnum)
                return "(" + f.ProtoType.FullCsType + ")" + ProtocolParser.Base + ".ReadUInt64(" + stream + ")";

            if (f.ProtoType is ProtoBuiltin)
            {
                switch (f.ProtoType.ProtoName)
                {
                    case ProtoBuiltin.Double:
                        return binaryReader + ".ReadDouble()";
                    case ProtoBuiltin.Float:
                        return binaryReader + ".ReadSingle()";
                    case ProtoBuiltin.Int32: //Wire format is 64 bit varint
                        return "(int)" + ProtocolParser.Base + ".ReadUInt64(" + stream + ")";
                    case ProtoBuiltin.Int64:
                        return "(long)" + ProtocolParser.Base + ".ReadUInt64(" + stream + ")";
                    case ProtoBuiltin.UInt32:
                        return ProtocolParser.Base + ".ReadUInt32(" + stream + ")";
                    case ProtoBuiltin.UInt64:
                        return ProtocolParser.Base + ".ReadUInt64(" + stream + ")";
                    case ProtoBuiltin.SInt32:
                        return ProtocolParser.Base + ".ReadZInt32(" + stream + ")";
                    case ProtoBuiltin.SInt64:
                        return ProtocolParser.Base + ".ReadZInt64(" + stream + ")";
                    case ProtoBuiltin.Fixed32:
                        return binaryReader + ".ReadUInt32()";
                    case ProtoBuiltin.Fixed64:
                        return binaryReader + ".ReadUInt64()";
                    case ProtoBuiltin.SFixed32:
                        return binaryReader + ".ReadInt32()";
                    case ProtoBuiltin.SFixed64:
                        return binaryReader + ".ReadInt64()";
                    case ProtoBuiltin.Bool:
                        return ProtocolParser.Base + ".ReadBool(" + stream + ")";
                    case ProtoBuiltin.String:
                        return ProtocolParser.Base + ".ReadString(" + stream + ")";
                    case ProtoBuiltin.Bytes:
                        return ProtocolParser.Base + ".ReadBytes(" + stream + ")";
                    default:
                        throw new ProtoFormatException("unknown build in: " + f.ProtoType.ProtoName, f.Source);
                }

            }

            throw new NotImplementedException();
        }

        #endregion

        #region Writer

        static void KeyWriter(string stream, int id, Wire wire, CodeWriter cw)
        {
            uint n = ((uint)id << 3) | ((uint)wire);
            cw.Comment("Key for field: " + id + ", " + wire);
            //cw.WriteLine("ProtocolParser.WriteUInt32(" + stream + ", " + n + ");");
            VarintWriter(stream, n, cw);
        }

        /// <summary>
        /// Generates writer for a varint value known at compile time
        /// </summary>
        static void VarintWriter(string stream, uint value, CodeWriter cw)
        {
            while (true)
            {
                byte b = (byte)(value & 0x7F);
                value = value >> 7;
                if (value == 0)
                {
                    cw.WriteLine(stream + ".WriteByte(" + b + ");");
                    break;
                }

                //Write part of value
                b |= 0x80;
                cw.WriteLine(stream + ".WriteByte(" + b + ");");
            }
        }

        /// <summary>
        /// Generates inline writer of a length delimited byte array
        /// </summary>
        static void BytesWriter(Field f, string stream, CodeWriter cw)
        {
            cw.Comment("Length delimited byte array");

            //Original
            //cw.WriteLine("ProtocolParser.WriteBytes(" + stream + ", " + memoryStream + ".ToArray());");

            //Much slower than original
            /*
            cw.WriteLine("ProtocolParser.WriteUInt32(" + stream + ", (uint)" + memoryStream + ".Length);");
            cw.WriteLine(memoryStream + ".Seek(0, System.IO.SeekOrigin.Begin);");
            cw.WriteLine(memoryStream + ".CopyTo(" + stream + ");");
            */

            //Same speed as original
            /*
            cw.WriteLine("ProtocolParser.WriteUInt32(" + stream + ", (uint)" + memoryStream + ".Length);");
            cw.WriteLine(stream + ".Write(" + memoryStream + ".ToArray(), 0, (int)" + memoryStream + ".Length);");
            */

            //10% faster than original using GetBuffer rather than ToArray
            cw.WriteLine("uint length" + f.ID + " = (uint)msField.Length;");
            cw.WriteLine(ProtocolParser.Base + ".WriteUInt32(" + stream + ", length" + f.ID + ");");
            cw.WriteLine("msField.WriteTo(" + stream + ");");
        }

        /// <summary>
        /// Generates code for writing one field
        /// </summary>
        public void FieldWriter(ProtoMessage m, Field f, CodeWriter cw, Options options)
        {
            if (f.Rule == FieldRule.Repeated)
            {
                //Repeated not packet
                cw.IfBracket("instance." + f.CsName + " != null");
                cw.ForeachBracket("var i" + f.ID + " in instance." + f.CsName);
                KeyWriter("stream", f.ID, f.ProtoType.WireType, cw);
                FieldWriterType(f, "stream", "bw", "i" + f.ID, cw);
                cw.EndBracket();
                cw.EndBracket();
            }
            else if (f.Rule == FieldRule.Optional)
            {
                if (options.Nullable ||
                    f.ProtoType is ProtoMessage ||
                    f.ProtoType.ProtoName == ProtoBuiltin.String ||
                    f.ProtoType.ProtoName == ProtoBuiltin.Bytes)
                {
                    if (f.ProtoType.Nullable || options.Nullable) //Struct always exist, not optional
                        cw.IfBracket("instance." + f.CsName + " != null");
                    KeyWriter("stream", f.ID, f.ProtoType.WireType, cw);
                    var needValue = !f.ProtoType.Nullable && options.Nullable;
                    FieldWriterType(f, "stream", "bw", "instance." + f.CsName + (needValue ? ".Value" : ""), cw);
                    if (f.ProtoType.Nullable || options.Nullable) //Struct always exist, not optional
                        cw.EndBracket();
                    return;
                }
                if (f.ProtoType is ProtoEnum)
                {
                    KeyWriter("stream", f.ID, f.ProtoType.WireType, cw);
                    FieldWriterType(f, "stream", "bw", "instance." + f.CsName, cw);
                    return;
                }
                KeyWriter("stream", f.ID, f.ProtoType.WireType, cw);
                FieldWriterType(f, "stream", "bw", "instance." + f.CsName, cw);
                return;
            }
            else if (f.Rule == FieldRule.Required)
            {
                if (f.ProtoType is ProtoMessage ||
                    f.ProtoType.ProtoName == ProtoBuiltin.String ||
                    f.ProtoType.ProtoName == ProtoBuiltin.Bytes)
                {
                    cw.WriteLine("if (instance." + f.CsName + " == null)");
                    cw.WriteIndent("throw new global::SilentOrbit.ProtocolBuffers.ProtocolBufferException(\"" + f.CsName + " is required by the proto specification.\");");
                }
                KeyWriter("stream", f.ID, f.ProtoType.WireType, cw);
                FieldWriterType(f, "stream", "bw", "instance." + f.CsName, cw);
                return;
            }
            throw new NotImplementedException("Unknown rule: " + f.Rule);
        }

        void FieldWriterType(Field f, string stream, string binaryWriter, string instance, CodeWriter cw)
        {
            cw.WriteLine(FieldWriterPrimitive(f, stream, binaryWriter, instance));
            return;
        }

        static string FieldWriterPrimitive(Field f, string stream, string binaryWriter, string instance)
        {
            if (f.ProtoType is ProtoEnum)
                return ProtocolParser.Base + ".WriteUInt64(" + stream + ",(ulong)" + instance + ");";

            if (f.ProtoType is ProtoMessage)
            {
                ProtoMessage pm = f.ProtoType as ProtoMessage;
                CodeWriter cw = new CodeWriter();
                cw.WriteLine("msField.SetLength(0);");
                cw.WriteLine(pm.FullSerializerType + ".Serialize(msField, " + instance + ");");
                BytesWriter(f, stream, cw);
                return cw.Code;
            }

            switch (f.ProtoType.ProtoName)
            {
                case ProtoBuiltin.Double:
                case ProtoBuiltin.Float:
                case ProtoBuiltin.Fixed32:
                case ProtoBuiltin.Fixed64:
                case ProtoBuiltin.SFixed32:
                case ProtoBuiltin.SFixed64:
                    return binaryWriter + ".Write(" + instance + ");";
                case ProtoBuiltin.Int32: //Serialized as 64 bit varint
                    return ProtocolParser.Base + ".WriteUInt64(" + stream + ",(ulong)" + instance + ");";
                case ProtoBuiltin.Int64:
                    return ProtocolParser.Base + ".WriteUInt64(" + stream + ",(ulong)" + instance + ");";
                case ProtoBuiltin.UInt32:
                    return ProtocolParser.Base + ".WriteUInt32(" + stream + ", " + instance + ");";
                case ProtoBuiltin.UInt64:
                    return ProtocolParser.Base + ".WriteUInt64(" + stream + ", " + instance + ");";
                case ProtoBuiltin.SInt32:
                    return ProtocolParser.Base + ".WriteZInt32(" + stream + ", " + instance + ");";
                case ProtoBuiltin.SInt64:
                    return ProtocolParser.Base + ".WriteZInt64(" + stream + ", " + instance + ");";
                case ProtoBuiltin.Bool:
                    return ProtocolParser.Base + ".WriteBool(" + stream + ", " + instance + ");";
                case ProtoBuiltin.String:
                    return ProtocolParser.Base + ".WriteBytes(" + stream + ", Encoding.UTF8.GetBytes(" + instance + "));";
                case ProtoBuiltin.Bytes:
                    return ProtocolParser.Base + ".WriteBytes(" + stream + ", " + instance + ");";
            }

            throw new NotImplementedException();
        }

        #endregion
    }
}

