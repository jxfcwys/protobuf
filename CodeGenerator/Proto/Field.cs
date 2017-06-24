using System;

namespace SilentOrbit.ProtocolBuffers
{
    class Field : IComment
    {
        public readonly SourcePath Source;

        public Field(TokenReader tr)
        {
            Source = new SourcePath(tr);
        }

        #region .proto data

        /// <summary>
        /// Comments written before the field in the .proto file.
        /// These comments will be written into the generated code.
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// required/optional/repeated as read from .proto file
        /// </summary>
        public FieldRule Rule { get; set; }

        /// <summary>
        /// Field type as read from the .proto file
        /// </summary>
        public string ProtoTypeName { get; set; }

        /// <summary>
        /// Field name read from the .proto file
        /// </summary>
        public string ProtoName { get; set; }

        /// <summary>
        /// Field name in generated c# code.
        /// </summary>
        public string CsName { get; set; }

        /// <summary>
        /// Wire format ID
        /// </summary>
        public int ID { get; set; }


        #region Code Generation Properties

        //These are generated as a second stage parsing of the .proto file.
        //They are used in the code generation.
        /// <summary>
        /// .proto type including enum and message.
        /// </summary>
        public ProtoType ProtoType { get; set; }

        #endregion

        /// <summary>
        /// Format the specified value according to the field type.
        /// </summary>
        /// <returns>String that can be use to assign to field of this field's type.</returns>
        /// <param name="value">Value.</param>
        public string FormatDefaultForTypeAssignment()
        {
            if (this.ProtoType is ProtoMessage || this.ProtoType is ProtoEnum)
            {
                if (this.Rule == FieldRule.Repeated)
                {
                    return string.Format("new List<{0}>()", this.ProtoType.FullCsType);
                }
                else
                {
                    return string.Format("new {0}()", this.ProtoType.FullCsType);
                }
            }
            else if (this.Rule == FieldRule.Repeated)
            {
                return string.Format("new List<{0}>()", this.ProtoType.CsType);
            }
            return string.Empty;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} = {3}", Rule, ProtoTypeName, ProtoName, ID);
        }

        #endregion
    }
}

