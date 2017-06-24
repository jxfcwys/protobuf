using System;
using System.Collections.Generic;

namespace SilentOrbit.ProtocolBuffers
{
    /// <summary>
    /// Parses local feature setting from the comments of the .proto file.
    /// </summary>
    internal static class LocalParser
    {
        public static void ParseComments(IComment message, List<string> comments, TokenReader tr)
        {
            message.Comments = "";
            foreach (string s in comments)
            {
                if (s.StartsWith(":"))
                {
                    try
                    {
                        string line = s.Substring(1);

                        //Remove comments after "//"
                        int cpos = line.IndexOf("//");
                        if (cpos >= 0)
                            line = line.Substring(0, cpos);

                        string[] parts = line.Split('=');
                        if (parts.Length > 2)
                            throw new ProtoFormatException("Bad option format, at most one '=', " + s, tr);                        
                        string value = (parts.Length == 2) ? parts[1].Trim() : null;
                        continue;
                    }
                    catch (Exception e)
                    {
                        throw new ProtoFormatException(e.Message, e, tr);
                    }
                }
                else
                {
                    message.Comments += s + "\n";
                }
            }
            message.Comments = message.Comments.Trim(new char[] { '\n' }).Replace("\n", "\r\n");
            comments.Clear();
        }
    }
}

