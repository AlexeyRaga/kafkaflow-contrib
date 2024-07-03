using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using Avro;
using Avro.Util;
using KafkaFlow.CryptoShredding.Avro;
using Avro.LogicalTypes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CSharp;

namespace KafkaFlow.CryptoShredding.Avro.Analyzers;

[Generator]
public class AvroSourceGenerator : ISourceGenerator
{
    private static readonly CodeGeneratorOptions CodeGenOptions =
        new() { BracingStyle = "C", BlankLinesBetweenMembers = false };

    public void Initialize(GeneratorInitializationContext context)
    {
        LogicalTypeFactory.Instance.Register(new InlineEncryptedStringLogicalType());
        LogicalTypeFactory.Instance.Register(new EncryptedStringLogicalType());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var codeGen = new CodeGen();

        var schemas = context.AdditionalFiles
            .Where(x => Path.GetExtension(x.Path).Equals(".avsc", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.GetText())
            .Select(x => x is null ? null : Schema.Parse(x.ToString()))
            .Where(x => x is not null);

        foreach (var schema in schemas) codeGen.AddSchema(schema);

        GenerateCodeForTypes(context, codeGen.GenerateCode());
    }

    // There is a bug in .NET avrogen 'GetTypes': it doesn't correctly handle types with the same
    // name but in different namespaces.
    // This method uses namespaces as folders to avoid this issue.
    private static void GenerateCodeForTypes(GeneratorExecutionContext context, CodeCompileUnit code)
    {
        using var provider = new CSharpCodeProvider();

        for (var i = 0; i < code.Namespaces.Count; i++)
        {
            var ns = code.Namespaces[i];
            var path = CodeGenUtil.Instance.UnMangle(ns.Name).Split('.').Aggregate(Path.Combine);

            var newNs = new CodeNamespace(ns.Name);
            newNs.Comments.Add(CodeGenUtil.Instance.FileComment);
            newNs.Imports.AddRange(CodeGenUtil.Instance.NamespaceImports);

            for (var j = 0; j < ns.Types.Count; j++)
            {
                var type = ns.Types[j];
                newNs.Types.Add(type);
                using (var writer = new StringWriter())
                {
                    var fileName = Path.ChangeExtension(CodeGenUtil.Instance.UnMangle(type.Name), "cs");
                    provider.GenerateCodeFromNamespace(newNs, writer, CodeGenOptions);
                    context.AddSource(Path.Combine(path, fileName), SourceText.From(writer.ToString(), Encoding.UTF8));
                }

                newNs.Types.Clear();
            }
        }
    }
}
