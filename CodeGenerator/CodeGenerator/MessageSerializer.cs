using System;
using SilentOrbit.Code;

namespace SilentOrbit.ProtocolBuffers
{
    class MessageSerializer
    {
        readonly CodeWriter cw;
        readonly Options options;
        readonly FieldSerializer fieldSerializer;

        public MessageSerializer(CodeWriter cw, Options options)
        {
            this.cw = cw;
            this.options = options;
            this.fieldSerializer = new FieldSerializer(cw, options);
        }

        public void GenerateClassSerializer(ProtoMessage m)
        {
            if (options.NoGenerateImported && m.IsImported)
            {
                Console.Error.WriteLine("Skipping imported " + m.FullProtoName);   
                return;
            }
            else
            {
                cw.Attribute("System.Serializable");
                cw.Bracket("public partial class " + m.SerializerType);
            }

            GenerateReader(m);

            GenerateWriter(m);
            foreach (ProtoMessage sub in m.Messages.Values)
            {
                cw.WriteLine();
                GenerateClassSerializer(sub);
            }
            cw.EndBracket();
            cw.WriteLine();
            return;
        }

        string ConvertReader(Field field, string strParam)
        {
            if (field.ProtoType is ProtoEnum)
            {
                return string.Format("({0})Convert.ToUInt64({1})", field.ProtoType.FullCsType, strParam);
            }
            else
            {
                switch (field.ProtoType.ProtoName)
                {
                    case ProtoBuiltin.Bool: return "Convert.ToBoolean(" + strParam + ")";
                    case ProtoBuiltin.Int32: return "Convert.ToInt32(" + strParam + ")";
                    case ProtoBuiltin.Int64: return "Convert.ToInt64(" + strParam + ")";
                    case ProtoBuiltin.UInt32: return "Convert.ToUInt32(" + strParam + ")";
                    case ProtoBuiltin.UInt64: return "Convert.ToUInt64(" + strParam + ")"; 
                    case ProtoBuiltin.Float: return "Convert.ToSingle(" + strParam + ")";
                    case ProtoBuiltin.Double: return "Convert.ToDouble(" + strParam + ")";
                    case ProtoBuiltin.String: return "Convert.ToString(" + strParam + ")";
                }
            }
            return string.Empty;
        }

        void GenerateReader(ProtoMessage m)
        {
            cw.Summary("Takes the remaining content of the json and deserialze it into the instance.");
            cw.Bracket("public static " + m.FullCsType + " Deserialize(SimpleJson.JsonObject json, " + m.FullCsType + " instance)");
            cw.WriteLine();
            //Prepare if field exist.
            cw.Comment("Assign Values");
            foreach (Field f in m.Fields.Values)
            {
                cw.WriteLine("instance.Has" + f.CsName + " = json.ContainsKey(\"" + f.ProtoName +"\");");
            }
            cw.WriteLine();

            //Parse values
            foreach (Field f in m.Fields.Values)
            {
                string line = "if (instance.Has" + f.CsName + "){";
                if (f.Rule == FieldRule.Repeated)
                {
                    line += " SimpleJson.JsonArray jsonArray = json[\"" + f.ProtoName + "\"] as SimpleJson.JsonArray;";
                    line += " if (jsonArray != null){";
                    line += " foreach (var v in jsonArray){";
                    if (f.ProtoType is ProtoMessage)
                    {
                        line += " " + f.ProtoType.FullCsType + " ins = new " + f.ProtoType.FullCsType + "();";
                        line += " instance." + f.CsName + ".Add(" + f.ProtoType.FullCsType + ".Deserialize((SimpleJson.JsonObject)v,  ins));";
                    }
                    else
                    {
                        line += " instance." + f.CsName + ".Add(" + ConvertReader(f, "v") + ");";
                    }
                    line += " }}";                    
                }
                else if (f.ProtoType is ProtoMessage)
                {
                    line += " instance." + f.CsName + " = new " + f.ProtoType.FullCsType + "();";
                    line += " " + f.ProtoType.FullCsType + ".Deserialize(json[\"" + f.ProtoName + "\"],  instance." + f.CsName + ");";
                }
                else
                {                    
                    line += " instance." + f.CsName + " = " + ConvertReader(f, "json[\"" + f.ProtoName + "\"]") + ";";
                }

                line += " }";
                if (f.Rule == FieldRule.Required)
                {
                    line += "else{";                    
                    line += " if (!json.ContainsKey(\"code\") || Convert.ToInt32(json[\"code\"]) == 0){";
                    line += " throw new ArgumentNullException(\"" + f.ProtoName + "\", \"Required by proto specification.\");";
                    line += " }}";                    
                }
                cw.WriteLine(line);
            }
            cw.WriteLine();
            cw.WriteLine("return instance;");
            cw.EndBracket();
        }

        /// <summary>
        /// Generates code for writing a class/message
        /// </summary>
        void GenerateWriter(ProtoMessage m)
        {
            cw.Summary("Serialize the instance into the json");
            cw.Bracket("public static SimpleJson.JsonObject Serialize(" + m.CsType + " instance)");
            cw.WriteLine("SimpleJson.JsonObject json = new SimpleJson.JsonObject();");

            cw.Comment("Assign values");
            foreach (Field f in m.Fields.Values)
            {
                if (f.Rule == FieldRule.Required && f.ProtoType.Nullable)
                {
                    cw.WriteLine("if (instance." + f.CsName + " == null)");
                    cw.WriteLine("    { throw new ArgumentNullException(\"" + f.CsName +"\", \"Required by proto specification.\"); }");
                }
                if (f.Rule == FieldRule.Repeated)
                {
                    cw.Bracket();
                    cw.WriteLine("SimpleJson.JsonArray jsonArray = new SimpleJson.JsonArray();");
                    cw.WriteLine("foreach(var v in instance." + f.CsName + ")");
                    if (f.ProtoType is ProtoMessage)
                    {
                        cw.WriteLine("    jsonArray.Add(" + f.ProtoType.FullCsType + ".Serialize(v));");
                    }
                    else
                    {
                        cw.WriteLine("    jsonArray.Add(v);");
                    }
                    cw.WriteLine("json[\"" + f.ProtoName + "\"] = jsonArray;");
                    cw.EndBracket();
                }
                else if (f.ProtoType is ProtoMessage)
                {
                    cw.WriteLine("json[\"" + f.ProtoName + "\"] = " + f.ProtoType.FullCsType + ".Serialize(instance." + f.CsName + ");");
                }
                else
                {
                    cw.WriteLine("json[\"" + f.ProtoName + "\"] = instance." + f.CsName + ";");
                }                
            }
            cw.WriteLine("return json;");
            cw.EndBracket();
        }
    }
}

