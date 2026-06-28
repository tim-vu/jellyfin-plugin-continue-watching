using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Jellyfin.Plugin.ContinueWatching.Controllers;

public sealed class RemoveActionConvention(string controllerFullName, string actionName) : IApplicationModelConvention
{
    private readonly string _controllerFullName = controllerFullName;
    private readonly string _actionName = actionName;

    public void Apply(ApplicationModel application)
    {
        var controller = application.Controllers.SingleOrDefault(
            controller => controller.ControllerType.FullName == _controllerFullName);

        if (controller is null)
        {
            return;
        }

        var actionsToRemove = controller.Actions
            .Where(action => action.ActionMethod.Name == _actionName)
            .ToList();

        foreach (ActionModel action in actionsToRemove)
        {
            controller.Actions.Remove(action);
        }
    }
}