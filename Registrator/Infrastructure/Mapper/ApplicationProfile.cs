using System.Reflection;
using AutoMapper;

public class ApplicationProfile : Profile
{
    public ApplicationProfile()
    {
        ApplyMappingsFrom(Assembly.GetExecutingAssembly());
        ApplyMappingsTo(Assembly.GetExecutingAssembly());
    }

    private void ApplyMappingsFrom(Assembly assembly)
    {
        ApplyMappings(assembly, typeof(IMapFrom<>), "MappingFrom");
    }

    private void ApplyMappingsTo(Assembly assembly)
    {
        ApplyMappings(assembly, typeof(IMapTo<>), "MappingTo");
    }

    private void ApplyMappings(Assembly assembly, Type mappingInterface, string methodName)
    {
        var types = assembly.GetExportedTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == mappingInterface))
            .ToList();

        var mappingInterfaceName = mappingInterface.Name;

        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type);

            var methodInfo = type.GetMethod(methodName)
                             ?? type.GetInterface(mappingInterfaceName)?.GetMethod(methodName);

            methodInfo?.Invoke(instance, new object[] { this });
        }
    }
}