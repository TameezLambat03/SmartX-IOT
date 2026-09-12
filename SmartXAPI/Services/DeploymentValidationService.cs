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

        DeploymentNode root =
            new DeploymentNode(segments[0].Trim());

        DeploymentNode current = root;

        for (int i = 1; i < segments.Length; i++)
        {
            DeploymentNode nextNode =
                new DeploymentNode(segments[i].Trim());

            current.AddChild(nextNode);

            current = nextNode;
        }

        return root;
    }
    //recursion
    public bool ValidateDeploymentRecursively(
        DeploymentNode? node,
        int depth = 0)
    {
        if (node == null)
        {
            return false;
        }

        // Every deployment node must have a valid name.
        if (string.IsNullOrWhiteSpace(node.Name))
        {
            return false;
        }

        // Prevent excessively deep or malformed deployment paths.
        const int maximumDepth = 10;

        if (depth >= maximumDepth)
        {
            return false;
        }

        // A deployment must contain at least three levels,
        // for example: Facility -> Zone -> Node.
        if (depth == 0 && node.Children.Count == 0)
        {
            return false;
        }

        // Recursively validate every child branch.
        foreach (DeploymentNode child in node.Children)
        {
            if (!ValidateDeploymentRecursively(
                    child,
                    depth + 1))
            {
                return false;
            }
        }

        return true;
    }
}