using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Avro;
using Avro.Util;
using Avro.LogicalTypes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CSharp;

namespace KafkaFlow.CryptoShredding.Avro.Analyzers;

[Generator]
public class AvroSourceGenerator : IIncrementalGenerator
{
    private static readonly CodeGeneratorOptions CodeGenOptions =
        new() { BracingStyle = "C", BlankLinesBetweenMembers = false };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        LogicalTypeFactory.Instance.Register(new InlineEncryptedStringLogicalType());
        LogicalTypeFactory.Instance.Register(new EncryptedStringLogicalType());

        var schemas = context.AdditionalTextsProvider
            .Where(x => Path.GetExtension(x.Path).Equals(".avsc", StringComparison.OrdinalIgnoreCase))
            .Select((text, cancellationToken) => text.GetText(cancellationToken)?.ToString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select((schemaText, _) => Schema.Parse(schemaText!))
            .Collect();

        context.RegisterSourceOutput(schemas, GenerateCode);
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<Schema> schemas)
    {
        if (schemas.IsDefaultOrEmpty)
            return;

        var codeGen = new CodeGen();
        foreach (var schema in schemas)
            codeGen.AddSchema(schema);

        GenerateCodeForTypes(context, codeGen.GenerateCode());
    }

    private static void GenerateCodeForTypes(SourceProductionContext context, CodeCompileUnit code)
    {
        using var provider = new CSharpCodeProvider();

        foreach (CodeNamespace ns in code.Namespaces)
        {
            var path = CodeGenUtil.Instance.UnMangle(ns.Name).Split('.').Aggregate(Path.Combine);

            var newNs = new CodeNamespace(ns.Name);
            newNs.Comments.Add(CodeGenUtil.Instance.FileComment);
            newNs.Imports.AddRange(CodeGenUtil.Instance.NamespaceImports);

            foreach (CodeTypeDeclaration type in ns.Types)
            {
                newNs.Types.Add(type);

                using var writer = new StringWriter();
                var fileName = Path.ChangeExtension(CodeGenUtil.Instance.UnMangle(type.Name), ".cs");

                var nsName = CodeGenUtil.Instance.UnMangle(ns.Name);
                var typName = CodeGenUtil.Instance.UnMangle(type.Name);
                var hintName = $"{nsName}.{typName}.g.cs".Replace(" ", "_");
                provider.GenerateCodeFromNamespace(newNs, writer, CodeGenOptions);

                context.AddSource(hintName, SourceText.From(writer.ToString(), Encoding.UTF8));

                // context.AddSource(Path.Combine(path, fileName), SourceText.From(writer.ToString(), Encoding.UTF8));
                newNs.Types.Clear();
            }
        }
    }
}
