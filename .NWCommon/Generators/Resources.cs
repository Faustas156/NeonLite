using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System.ComponentModel;
using System.Linq;
using System.IO;
using System;

[Generator]
public class ResourcesCodeGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var basePath);

        var files = context.AdditionalFiles
                .Where(x =>
                {
                    context.AnalyzerConfigOptions.GetOptions(x)
                        .TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var sig);
                    return sig == "NWCResource";
                })
                .Select(x =>
                {
                    // Uri requires paths to end with a separator to treat them as directories
                    Uri baseUri = new(basePath.EndsWith("\\") ? basePath : basePath + "\\");
                    Uri fullUri = new(x.Path);

                    var relative = baseUri.MakeRelativeUri(fullUri).ToString()
                        .Replace('/', Path.DirectorySeparatorChar);

                    var manifest = baseUri.MakeRelativeUri(fullUri).ToString()
                        .Replace('/', '.');

                    relative = string.Join(Path.DirectorySeparatorChar.ToString(),
                        relative.Split(Path.DirectorySeparatorChar).Skip(1));

                    if (relative.StartsWith("Resources"))
                    {
                        relative = string.Join(Path.DirectorySeparatorChar.ToString(),
                            relative.Split(Path.DirectorySeparatorChar).Skip(1));
                    }
                    return (relative, manifest);
                });

        var sb = new StringBuilder();
        sb.Append($$"""
            using System.Reflection;
            using System.Text;
            namespace {{rootNamespace}}
            {
                class Resource
                {
                    internal Resource(string path) => this.path = path;
                    readonly string path;
                    static Assembly assembly;

                    static Assembly GetAssembly()
                    {
                        if (assembly == null)
                            assembly = Assembly.GetExecutingAssembly();
                        return assembly;
                    }

                    public string GetUTF8String()
                    {
                        var stream = GetAssembly().GetManifestResourceStream(path);
                        using StreamReader reader = new(stream, Encoding.UTF8);
                        return reader.ReadToEnd();
                    }
                    public byte[] GetBytes()
                    {
                        var stream = GetAssembly().GetManifestResourceStream(path);
                        var ret = new byte[stream.Length];
                        stream.Read(ret, 0, (int)stream.Length);
                        return ret;
                    }

                    public Stream GetStream() => GetAssembly().GetManifestResourceStream(path);
                }

                internal static class Resources
                {
            """);
        sb.AppendLine("");

        var iter = files
            .OrderByDescending(
                x => x.relative.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar))
            .OrderBy(x => x.relative);

        string prevFolder = "";
        foreach ((var file, var manifest) in iter)
        {
            var dirname = Path.GetDirectoryName(file) ?? "";
            while (dirname != prevFolder)
            {
                if (dirname.StartsWith(prevFolder))
                {
                    var first = dirname
                        .Substring(prevFolder.Length + 1)
                        .Split(Path.DirectorySeparatorChar).First();

                    sb.AppendLine($"public static class {first} {{");
                    prevFolder = Path.Combine(prevFolder, first);
                }
                else if (prevFolder != null)
                {
                    sb.AppendLine("}");
                    prevFolder = Path.GetDirectoryName(prevFolder) ?? "";
                }

            }

            var resname = Path.GetFileNameWithoutExtension(file);
            resname = resname.Replace("-", "_");
            resname = resname.Replace(".", "_");
            sb.AppendLine($"public readonly static Resource {resname} = new(\"{manifest}\");");

            prevFolder = dirname;
        }

        sb.Append("""
                        }
                    }
                    """);

        // generate a class that contains their values as const strings
        context.AddSource("NWCResources", sb.ToString());
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}
