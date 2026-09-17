using System.Xml.Linq;
using Xunit;

namespace BookingSystem.ArchitectureTests;

public sealed class AspireBoundaryTests
{
    [Fact]
    public void BusinessLayersDoNotReferenceAspire()
    {
        var root = FindRepositoryRoot();
        var projects = Directory.EnumerateFiles(Path.Combine(root, "src", "Modules"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.Exists(Path.Combine(root, "src", "Contracts"))
                ? Directory.EnumerateFiles(Path.Combine(root, "src", "Contracts"), "*.csproj", SearchOption.AllDirectories)
                : []);

        foreach (var project in projects)
        {
            var layer = Path.GetFileNameWithoutExtension(project);
            var document = XDocument.Load(project);
            var dependencies = document.Descendants()
                .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference")
                .Select(element => (string?)element.Attribute("Include"))
                .Where(value => value is not null)
                .ToArray();

            if (layer.EndsWith(".Domain", StringComparison.Ordinal)
                || layer.EndsWith(".Application", StringComparison.Ordinal)
                || project.Contains($"{Path.DirectorySeparatorChar}Contracts{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                Assert.DoesNotContain(dependencies, dependency => dependency!.Contains("Aspire", StringComparison.OrdinalIgnoreCase)
                    || dependency.Contains("ServiceDefaults", StringComparison.OrdinalIgnoreCase)
                    || dependency.Contains("Observability", StringComparison.OrdinalIgnoreCase));
            }

            if (layer.EndsWith(".Infrastructure", StringComparison.Ordinal))
            {
                Assert.DoesNotContain(dependencies, dependency => dependency!.Contains("BookingSystem.AppHost", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void ServiceDefaultsDoesNotReferenceBusinessModules()
    {
        var root = FindRepositoryRoot();
        var project = Path.Combine(root, "src", "BuildingBlocks", "BookingSystem.ServiceDefaults", "BookingSystem.ServiceDefaults.csproj");
        var references = XDocument.Load(project).Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => (string?)element.Attribute("Include"));

        Assert.DoesNotContain(references, reference => reference?.Contains("Modules", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void ObservabilityDoesNotReferenceBusinessModulesOrAspire()
    {
        var root = FindRepositoryRoot();
        var project = Path.Combine(root, "src", "BuildingBlocks", "BookingSystem.Observability", "BookingSystem.Observability.csproj");
        var references = XDocument.Load(project).Descendants()
            .Where(element => element.Name.LocalName is "ProjectReference" or "PackageReference")
            .Select(element => (string?)element.Attribute("Include"));

        Assert.DoesNotContain(references, reference => reference?.Contains("Modules", StringComparison.OrdinalIgnoreCase) == true
            || reference?.Contains("Aspire", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BookingSystem.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
