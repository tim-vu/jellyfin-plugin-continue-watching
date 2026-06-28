using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System;

namespace Jellyfin.Plugin.HomeScreenSections.Client;

public sealed class SectionsClient : ISectionsClient
{
    private const string PluginAssemblyName = "Jellyfin.Plugin.HomeScreenSections";
    private const string PluginInterfaceTypeName = "Jellyfin.Plugin.HomeScreenSections.PluginInterface";
    private const string RegisterMethodName = "RegisterSection";
    private const string ResultsMethodName = nameof(ISectionResultsProvider.GetResults);

    public bool IsAvailable => FindRegisterMethod() is not null;

    public bool TryRegisterSection<TResultsProvider>(SectionDefinition section)
        where TResultsProvider : class, ISectionResultsProvider
    {
        ArgumentNullException.ThrowIfNull(section);
        ValidateSection(section);

        MethodInfo? registerMethod = FindRegisterMethod();
        if (registerMethod is null)
        {
            return false;
        }

        Type resultsProviderType = typeof(TResultsProvider);
        if (!resultsProviderType.IsPublic || resultsProviderType.IsAbstract)
        {
            throw new ArgumentException(
                $"The results provider {resultsProviderType.FullName} must be a public, non-abstract class.",
                nameof(TResultsProvider));
        }

        try
        {
            object payload = CreatePayload(registerMethod, section, resultsProviderType);
            registerMethod.Invoke(null, [payload]);
            return true;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new SectionsException("Home Screen Sections rejected the section registration.", exception.InnerException);
        }
        catch (Exception exception) when (exception is ArgumentException or MethodAccessException)
        {
            throw new SectionsException("The loaded Home Screen Sections plugin has an incompatible registration API.", exception);
        }
    }

    private static MethodInfo? FindRegisterMethod()
    {
        Assembly? pluginAssembly = AssemblyLoadContext.All
            .SelectMany(context => context.Assemblies)
            .FirstOrDefault(assembly => string.Equals(
                assembly.GetName().Name,
                PluginAssemblyName,
                StringComparison.Ordinal));

        Type? pluginInterface = pluginAssembly?.GetType(PluginInterfaceTypeName, throwOnError: false);
        return pluginInterface?
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                string.Equals(method.Name, RegisterMethodName, StringComparison.Ordinal)
                && SupportsPayload(method));
    }

    private static bool SupportsPayload(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Length == 1
            && parameters[0].ParameterType.GetMethod(
                "Parse",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(string)],
                modifiers: null) is not null;
    }

    private static object CreatePayload(
        MethodInfo registerMethod,
        SectionDefinition section,
        Type resultsProviderType)
    {
        ParameterInfo[] parameters = registerMethod.GetParameters();
        if (parameters.Length != 1)
        {
            throw new SectionsException("The loaded HomeScreenSections plugin has an incompatible registration API.");
        }

        string json = JsonSerializer.Serialize(new
        {
            id = section.Id.ToString(),
            displayText = section.DisplayText,
            limit = section.Limit,
            route = section.Route,
            additionalData = section.AdditionalData,
            resultsAssembly = resultsProviderType.Assembly.FullName,
            resultsClass = resultsProviderType.FullName,
            resultsMethod = ResultsMethodName,
        });

        Type payloadType = parameters[0].ParameterType;
        MethodInfo? parseMethod = payloadType.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(string)],
            modifiers: null);

        return parseMethod?.Invoke(null, [json])
            ?? throw new SectionsException("The loaded Home Screen Sections plugin uses an unsupported payload type.");
    }

    private static void ValidateSection(SectionDefinition section)
    {
        if (section.Id == Guid.Empty)
        {
            throw new ArgumentException("A section ID must not be empty.", nameof(section));
        }

        if (string.IsNullOrWhiteSpace(section.DisplayText))
        {
            throw new ArgumentException("Section display text is required.", nameof(section));
        }

        if (section.Limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(section), "Section limit must be at least one.");
        }
    }
}