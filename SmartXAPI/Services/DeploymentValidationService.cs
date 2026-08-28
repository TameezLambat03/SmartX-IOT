using SmartX.Shared.Models;

namespace SmartXAPI.Services;

public class DeploymentValidationService
{
    public DeploymentNode BuildDeploymentHierarchy(string path)
    {
        string[] segments = path.Split(
            new[] { '/', '\\', '>' },
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return new DeploymentNode("Root");
        }

        DeploymentNode root = new DeploymentNode(segments[0].Trim());
        DeploymentNode current = root;

        for (int i = 1; i < segments.Length; i++)
        {
            DeploymentNode nextNode =
                new DeploymentNode(segments[i].Trim());

            current.Child = nextNode;
            current = nextNode;
        }

        return root;
    }

    public bool ValidateDeploymentRecursively(
        DeploymentNode? node,
        int depth = 0)
    {
        if (node == null)
        {
            return depth >= 2;
        }

        if (string.IsNullOrWhiteSpace(node.Name))
        {
            return false;
        }

        return ValidateDeploymentRecursively(
            node.Child,
            depth + 1);
    }
}