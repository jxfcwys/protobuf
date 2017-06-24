using System;
using SilentOrbit.Code;
using System.Collections.Generic;

namespace SilentOrbit.ProtocolBuffers
{
    class MessageCode
    {
        readonly CodeWriter cw;
        readonly Options options;

        public MessageCode(CodeWriter cw, Options options)
        {
            this.cw = cw;
            this.options = options;
        }

        public void GenerateClass(ProtoMessage m)
        {
            if (options.NoGenerateImported && m.IsImported)
            {
                Console.Error.WriteLine("Skipping imported " + m.FullProtoName);   
                return;
            }

            //Default class
            cw.Summary(m.Comments);
            cw.Bracket("public partial class " + m.CsType);

            GenerateCtor(m);

            GenerateEnums(m);

            GenerateProperties(m);

            GenerateClear(m);

            foreach (ProtoMessage sub in m.Messages.Values)
            {
                GenerateClass(sub);
                cw.WriteLine();
            }
            cw.EndBracket();
            return;
        }

        void GenerateCtor(ProtoMessage m)
        {
            // Collect all message fields.
            var mfields = new List<Field>();
            foreach (Field field in m.Fields.Values)
            {                
                if (field.Rule == FieldRule.Repeated || field.ProtoType is ProtoMessage)
                {
                    mfields.Add(field);
                }
            }

            cw.Bracket("public " + m.CsType + "()");
            if (mfields.Count > 0)
            {
                foreach (var field in mfields)
                {
                    string formattedValue = field.FormatDefaultForTypeAssignment();
                    string line = string.Format("{0} = {1};", field.CsName, formattedValue);
                    cw.WriteLine(line);
                }
            }
            cw.EndBracket();
            cw.WriteLine();
        }

        void GenerateEnums(ProtoMessage m)
        {
            foreach (ProtoEnum me in m.Enums.Values)
            {
                GenerateEnum(me);
            }
        }

        public void GenerateEnum(ProtoEnum m)
        {
            if (options.NoGenerateImported && m.IsImported)
            {
                Console.Error.WriteLine("Skipping imported enum " + m.FullProtoName);   
                return;
            }

            cw.Summary(m.Comments);
            if (m.OptionFlags)
                cw.Attribute("System.FlagsAttribute");
            cw.Bracket("public enum " + m.CsType);
            foreach (var epair in m.Enums)
            {
                cw.Summary(epair.Comment);
                cw.WriteLine(epair.Name + " = " + epair.Value + ",");
            }
            cw.EndBracket();
            cw.WriteLine();
        }

        /// <summary>
        /// Generates the properties.
        /// </summary>
        /// <param name='template'>
        /// if true it will generate only properties that are not included by default, because of the [generate=false] option.
        /// </param>
        void GenerateProperties(ProtoMessage m)
        {
            foreach (Field f in m.Fields.Values)
            {
                if (f.Comments != null)
                    cw.Summary(f.Comments);

                cw.WriteLine(GenerateProperty(f));
                cw.WriteLine();
            }

            //Add HasField
            foreach (Field f in m.Fields.Values)
            {
                cw.WriteLine("public bool Has" + f.CsName + " { get; set; }");
                cw.WriteLine();
            }

            //Wire format field ID
#if DEBUGx
            cw.Comment("ProtocolBuffers wire field id");
            foreach (Field f in m.Fields.Values)
            {
                cw.WriteLine("public const int " + f.CsName + "FieldID = " + f.ID + ";");
            }
#endif
        }

        string GenerateProperty(Field f)
        {
            string type = f.ProtoType.FullCsType;
            if (f.Rule == FieldRule.Repeated)
                type = "List<" + type + ">";
            return "public " + type + " " + f.CsName + " { get; set; }";
        }        

        void GenerateClear(ProtoMessage m)
        {
            cw.Bracket("public void Clear()");
            foreach (Field f in m.Fields.Values)
            {
                if (f.Rule == FieldRule.Repeated || f.ProtoType is ProtoMessage)
                {
                    cw.WriteLine(f.CsName + ".Clear();");
                }
                else if (f.ProtoType is ProtoEnum)
                {
                    cw.WriteLine(f.CsName + " = " + f.ProtoType.FullCsType + "." + (f.ProtoType as ProtoEnum).Enums[0].Name);
                }
                else
                {
                    switch (f.ProtoType.ProtoName)
                    {
                        case ProtoBuiltin.Bool:
                            cw.WriteLine(f.CsName + " = false;");
                            break;                    
                        case ProtoBuiltin.Int32:
                        case ProtoBuiltin.Int64:
                        case ProtoBuiltin.UInt32:
                        case ProtoBuiltin.UInt64:
                            cw.WriteLine(f.CsName + " = 0;");
                            break;
                        case ProtoBuiltin.Float:
                        case ProtoBuiltin.Double:
                            cw.WriteLine(f.CsName + " = 0f;");
                            break;
                        case ProtoBuiltin.String:
                            cw.WriteLine(f.CsName + " = string.Empty;");
                            break;
                    }
                }
            }
            cw.EndBracket();
            cw.WriteLine();
        }
    }
}

