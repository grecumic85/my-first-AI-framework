using System.Text.Json;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using NJsonSchema.CodeGeneration.CSharp;

if (args.Length != 1) throw new ArgumentException("Usage: ApiGenerator <absolute-or-relative-services.json>");
var manifestPath = Path.GetFullPath(args[0]);
var contractRoot = Path.GetDirectoryName(manifestPath)!;
var repoRoot = Path.GetFullPath(Path.Combine(contractRoot, ".."));
var outputRoot = Path.Combine(repoRoot, "src", "Automation.Generated");
var services = JsonSerializer.Deserialize<Service[]>(await File.ReadAllTextAsync(manifestPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Empty manifest.");
if (services.Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != services.Length)
    throw new InvalidOperationException("Service names must be unique.");
// Generate everything in memory first, so invalid contracts leave existing generated files intact.
var outputs = new List<(string Path, string Content)>();
foreach (var service in services)
{
    if (!System.Text.RegularExpressions.Regex.IsMatch(service.Name, "^[A-Z][A-Za-z0-9]*$")) throw new ArgumentException("Use PascalCase service names.");
    var input = Path.GetFullPath(Path.Combine(contractRoot, service.Spec));
    if (!input.StartsWith(contractRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Specifications must stay inside contracts/.");
    var document = await OpenApiDocument.FromFileAsync(input);
    var settings = new CSharpClientGeneratorSettings
    {
        ClassName = service.Name + "Client", GenerateClientInterfaces = true,
        InjectHttpClient = true, DisposeHttpClient = false, UseBaseUrl = false,
        GenerateBaseUrlProperty = false,
        CSharpGeneratorSettings =
        {
            Namespace = "Automation.Generated." + service.Name,
            JsonLibrary = CSharpJsonLibrary.SystemTextJson,
            GenerateNullableReferenceTypes = true
        }
    };
    var generated = new CSharpClientGenerator(document, settings).GenerateFile();
    outputs.Add((Path.Combine(outputRoot, service.Name + ".g.cs"), generated.Replace("\r\n", "\n")));
}
Directory.CreateDirectory(outputRoot);
foreach (var output in outputs)
{
    await File.WriteAllTextAsync(output.Path, output.Content);
    Console.WriteLine($"Generated {Path.GetFileName(output.Path)}");
}
record Service(string Name, string Spec);
