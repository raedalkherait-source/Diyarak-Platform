using System.Xml.Linq;
namespace Diyarak.Platform.Architecture.Tests;

public sealed class ArchitectureTests
{
    private static readonly string Root = FindRepositoryRoot();

    [Fact]
    public void Foundation_projects_use_approved_names()
    {
        string[] forbidden = ["Common", "Utils", "Helpers"];
        foreach (string project in Directory.EnumerateFiles(Path.Combine(Root, "src", "Foundation"), "*.csproj", SearchOption.AllDirectories))
            Assert.DoesNotContain(forbidden, word => Path.GetFileNameWithoutExtension(project).Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Domain_primitives_has_no_project_references()
    {
        string project = Path.Combine(Root, "src", "Foundation", "Diyarak.Platform.Domain.Primitives", "Diyarak.Platform.Domain.Primitives.csproj");
        Assert.Empty(ReadProjectReferences(project));
    }

    [Fact]
    public void Shared_kernel_only_references_domain_primitives()
    {
        string project = Path.Combine(Root, "src", "Foundation", "Diyarak.Platform.SharedKernel", "Diyarak.Platform.SharedKernel.csproj");
        string[] references = ReadProjectReferences(project);
        Assert.Single(references);
        Assert.EndsWith("Diyarak.Platform.Domain.Primitives.csproj", references[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Foundation_does_not_reference_core_modules_integrations_or_hosts()
    {
        string[] forbiddenSegments = ["/Core/", "/Modules/", "/Application/", "/Integrations/", "/Hosts/"];
        foreach (string project in Directory.EnumerateFiles(Path.Combine(Root, "src", "Foundation"), "*.csproj", SearchOption.AllDirectories))
            foreach (string reference in ReadProjectReferences(project))
            {
                string normalized = reference.Replace('\\', '/');
                Assert.DoesNotContain(forbiddenSegments, segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase));
            }
    }

    [Fact]
    public void Core_does_not_reference_modules_integrations_or_hosts()
    {
        string[] forbiddenSegments = ["/Modules/", "/Application/", "/Integrations/", "/Hosts/"];
        foreach (string project in Directory.EnumerateFiles(Path.Combine(Root, "src", "Core"), "*.csproj", SearchOption.AllDirectories))
            foreach (string reference in ReadProjectReferences(project))
            {
                string normalized = reference.Replace('\\', '/');
                Assert.DoesNotContain(forbiddenSegments, segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase));
            }
    }
    [Fact]
    public void Modules_only_reference_foundation_or_explicitly_approved_core_projects()
    {
        string[] forbiddenSegments = ["/Modules/", "/Application/", "/Integrations/", "/Hosts/"];
        string[] approvedCoreDependencies =
        [
            "Diyarak.Market.Listing.csproj->Diyarak.Platform.Listing.csproj",
        ];

        foreach (string project in Directory.EnumerateFiles(Path.Combine(Root, "src", "Modules"), "*.csproj", SearchOption.AllDirectories))
            foreach (string reference in ReadProjectReferences(project))
            {
                string normalized = reference.Replace('\\', '/');

                Assert.DoesNotContain(forbiddenSegments, segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase));

                if (normalized.Contains("/Core/", StringComparison.OrdinalIgnoreCase))
                {
                    string dependency = $"{Path.GetFileName(project)}->{Path.GetFileName(reference)}";
                    Assert.Contains(dependency, approvedCoreDependencies);
                }
            }
    }
    [Fact]
    public void Application_does_not_reference_integrations_or_hosts()
    {
        string[] forbiddenSegments = ["/Integrations/", "/Hosts/"];

        foreach (string project in Directory.EnumerateFiles(Path.Combine(Root, "src", "Application"), "*.csproj", SearchOption.AllDirectories))
            foreach (string reference in ReadProjectReferences(project))
            {
                string normalized = reference.Replace('\\', '/');
                Assert.DoesNotContain(forbiddenSegments, segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase));
            }
    }

    [Fact]
    public void Application_direct_core_dependencies_are_explicitly_approved()
    {
        string[] approvedCoreDependencies = [];

        foreach (string project in Directory.EnumerateFiles(Path.Combine(Root, "src", "Application"), "*.csproj", SearchOption.AllDirectories))
            foreach (string reference in ReadProjectReferences(project))
            {
                string normalized = reference.Replace('\\', '/');

                if (normalized.Contains("/Core/", StringComparison.OrdinalIgnoreCase))
                {
                    string dependency = $"{Path.GetFileName(project)}->{Path.GetFileName(normalized)}";
                    Assert.Contains(dependency, approvedCoreDependencies);
                }
            }
    }

    [Fact]
    public void Integrations_do_not_reference_hosts_or_other_integrations()
    {
        string[] forbiddenSegments = ["/Integrations/", "/Hosts/"];

        foreach (string project in Directory.EnumerateFiles(Path.Combine(Root, "src", "Integrations"), "*.csproj", SearchOption.AllDirectories))
            foreach (string reference in ReadProjectReferences(project))
            {
                string projectDirectory = Path.GetDirectoryName(project) ?? throw new InvalidOperationException("Integration project directory was not found.");
                string normalized = Path.GetFullPath(Path.Combine(projectDirectory, reference)).Replace('\\', '/');

                Assert.DoesNotContain(forbiddenSegments, segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase));
            }
    }

    private static string[] ReadProjectReferences(string project)
    {
        XDocument document = XDocument.Load(project);
        return document.Descendants("ProjectReference").Select(element => element.Attribute("Include")?.Value).Where(value => value is not null).Cast<string>().ToArray();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Diyarak.Platform.All.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
