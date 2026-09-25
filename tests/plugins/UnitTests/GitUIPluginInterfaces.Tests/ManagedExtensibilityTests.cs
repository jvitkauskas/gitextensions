using System.Reflection;
using GitUIPluginInterfaces;

namespace GitUIPluginInterfacesTests;
public class ManagedExtensibilityTests
{
    private static Assembly? Resolve(string name, Assembly? requestingAssembly)
    {
        MethodInfo resolver = typeof(ManagedExtensibility).GetMethod("CurrentDomain_AssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (Assembly?)resolver.Invoke(null, [null, new ResolveEventArgs(name, requestingAssembly)]);
    }

    [Test]
    public void Resolve_dependency_by_assembly_name()
    {
        Assembly dependency = typeof(ManagedExtensibility).Assembly;
        Resolve(dependency.FullName!, typeof(ManagedExtensibilityTests).Assembly).Should().BeSameAs(dependency);
    }

    [Test]
    public void Do_not_resolve_an_assembly_name_with_a_shared_prefix()
    {
        Assembly dependency = typeof(ManagedExtensibility).Assembly;
        Resolve($"{dependency.GetName().Name}.Missing, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", typeof(ManagedExtensibilityTests).Assembly).Should().BeNull();
    }

    [Test]
    public void Missing_satellite_assembly_allows_resource_fallback()
    {
        Resolve("GitUIPluginInterfaces.resources, Version=1.0.0.0, Culture=fr-FR, PublicKeyToken=null", typeof(ManagedExtensibility).Assembly).Should().BeNull();
    }

    [Test]
    public void Without_a_requesting_assembly_resolution_is_left_to_the_runtime()
    {
        Resolve(typeof(ManagedExtensibility).Assembly.FullName!, null).Should().BeNull();
    }

    [TestCase]
    public void ThrowWhenUserPluginsPathAlreadyInitialized()
    {
        ManagedExtensibility.SetUserPluginsPath("A");
        ((Action)(() => ManagedExtensibility.SetUserPluginsPath("B"))).Should().Throw<InvalidOperationException>();
    }
}
