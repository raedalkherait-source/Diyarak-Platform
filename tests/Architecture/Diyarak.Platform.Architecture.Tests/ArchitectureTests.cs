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
        string[] forbiddenSegments = ["/Core/", "/Modules/", "/Integrations/", "/Hosts/"];
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
        string[] forbiddenSegments = ["/Modules/", "/Integrations/", "/Hosts/"];
        foreach (string project in Directory.EnumerateFiles(Path.Combine(Root, "src", "Core"), "*.csproj", SearchOption.AllDirectories))
            foreach (string reference in ReadProjectReferences(project))
            {
                string normalized = reference.Replace('\\', '/');
                Assert.DoesNotContain(forbiddenSegments, segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase));
            }
    }
    [Fact]
    public void Modules_do_not_reference_other_modules_integrations_or_hosts()
    {
        string[] forbiddenSegments = ["/Modules/", "/Integrations/", "/Hosts/"];

        foreach (string project in Directory.EnumerateFiles(Path.Combine(Root, "src", "Modules"), "*.csproj", SearchOption.AllDirectories))
            foreach (string reference in ReadProjectReferences(project))
            {
                string normalized = reference.Replace('\\', '/');
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



