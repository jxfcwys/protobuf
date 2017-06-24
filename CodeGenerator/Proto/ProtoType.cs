using System;

namespace SilentOrbit.ProtocolBuffers
{
    /// <summary>
    /// A protobuf message or enum
    /// </summary>
    abstract class ProtoType
    {
        public ProtoMessage Parent { get; set; }

        /// <summary>
        /// Name used in the .proto file, 
        /// </summary>
        public string ProtoName { get; set; }

        /// <summary>
        /// Based on ProtoType and Rule according to the protocol buffers specification
        /// </summary>
        public abstract Wire WireType { get; }

        /// <summary>
        /// The c# type name
        /// </summary>
        public virtual string CsType { get; set; }

        /// <summary>
        /// The C# namespace for this item
        /// </summary>
        public virtual string CsNamespace
        {
            get
            {
                if (OptionNamespace == null)
                {
                    if (Parent is ProtoCollection)
                        return Parent.CsNamespace;
                    else
                        return Parent.CsNamespace + "." + Parent.CsType;
                }
                else
                    return OptionNamespace;
            }
        }

        public virtual string FullCsType
        {
            get { return CsNamespace + "." + CsType; }
        }

        /// <summary>
        /// The C# namespace for this item
        /// </summary>
        public virtual string FullProtoName
        {
            get
            {
                if (Parent is ProtoCollection)
                    return Package + "." + ProtoName;
                return Parent.FullProtoName + "." + ProtoName;
            }
        }

        /// <summary>
        /// .proto package option
        /// </summary>
        public string Package { get; set; }

        public string OptionNamespace { get; set; }

        /// <summary>
        /// Used by types within a namespace
        /// </summary>
        public ProtoType(ProtoMessage parent, string package)
            : this()
        {
            if (this is ProtoCollection == false)
            {
                if (parent == null)
                    throw new ArgumentNullException("parent");
                if (package == null)
                    throw new ArgumentNullException("package");
            }
            this.Parent = parent;
            this.Package = package;
        }

        public ProtoType()
        {
            this.OptionNamespace = null;
        }

        public bool Nullable
        {
            get
            {
                if (ProtoName == ProtoBuiltin.String)
                    return true;
                if (ProtoName == ProtoBuiltin.Bytes)
                    return true;
                if (this is ProtoMessage)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// If constant size, return the size, if not return -1.
        /// </summary>
        public virtual int WireSize
        {
            get
            {
                if (WireType == Wire.Fixed32)
                    return 4;
                if (WireType == Wire.Fixed64)
                    return 8;
                if (WireType == Wire.Varint)
                    return -1;
                if (WireType == Wire.LengthDelimited)
                    return -1;
                return -1;
            }
        }

        #region Imported Flag

        /// <summary>
        /// True if this class was imported rather being specified as an input argument.
        /// </summary>
        public bool IsImported { get; set; }

        public virtual void MarkImported()
        {
            IsImported = true;
        }

        #endregion
    }
}

