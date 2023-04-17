﻿namespace ResxEditor.Writers;

using System.Text;
using Resx;

public class DesignerWriter
{
    private const string HeaderContent = """
    //     This code was generated by a tool.
    //
    //     Changes to this file may cause incorrect behavior and will be lost if
    //     the code is regenerated.
    // </auto-generated>
    //------------------------------------------------------------------------------

    namespace <NAMESPACE> {
        using System;


        /// <summary>
        ///   A strongly-typed resource class, for looking up localized strings, etc.
        /// </summary>
        // This class was auto-generated by the StronglyTypedResourceBuilder
        // class via a tool like ResGen or Visual Studio.
        // To add or remove a member, edit your .ResX file then rerun ResGen
        // with the /str option, or rebuild your VS project.
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        internal class <CLASSNAME> {

            private static global::System.Resources.ResourceManager resourceMan;

            private static global::System.Globalization.CultureInfo resourceCulture;

            [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal Localization() {
            }

            /// <summary>
            ///   Returns the cached ResourceManager instance used by this class.
            /// </summary>
            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
            internal static global::System.Resources.ResourceManager ResourceManager {
                get {
                    if (object.ReferenceEquals(resourceMan, null)) {
                        global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("<NAMESPACE>.<CLASSNAME>", typeof(<CLASSNAME>).Assembly);
                        resourceMan = temp;
                    }
                    return resourceMan;
                }
            }

            /// <summary>
            ///   Overrides the current thread's CurrentUICulture property for all
            ///   resource lookups using this strongly typed resource class.
            /// </summary>
            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
            internal static global::System.Globalization.CultureInfo Culture {
                get {
                    return resourceCulture;
                }
                set {
                    resourceCulture = value;
                }
            }

    """;

    private const string EndContent = """
        }
    }
    """;

    /// <summary>
    /// Generates a *.Designer.cs file for the <see cref="ResxDocument"/> instance.
    /// </summary>
    /// <param name="document"></param>
    public static string Write(ResxDocument document)
    {
        var stringBuilder = new StringBuilder();

        // Write the header
        var header = HeaderContent.Replace("<CLASSNAME>", document.Name)
            .Replace("<NAMESPACE>", document.Namespace);
        stringBuilder.AppendLine(header);

        // Write the data
        foreach (var (key, value) in document.Values)
        {
            stringBuilder.AppendLine( "        /// <summary>");
            stringBuilder.AppendLine($"        ///   Looks up a localized string similar to {value}");
            stringBuilder.AppendLine( "        /// </summary>");
            stringBuilder.AppendLine($"        {document.AccessSpecifier} static string {key} {{");
            stringBuilder.AppendLine( "            get {");
            stringBuilder.AppendLine($"                return ResourceManager.GetString(\"{key}\", resourceCulture);");
            stringBuilder.AppendLine( "            }");
            stringBuilder.AppendLine( "        }");
            stringBuilder.AppendLine();
        }

        // Write the footer
        stringBuilder.AppendLine(EndContent);

        return stringBuilder.ToString();
    }
}
