namespace SmartX.Shared.Models;

public class DeploymentNode
{
    public string Name { get; set; }
    public DeploymentNode? Child { get; set; }

    public DeploymentNode(string name, DeploymentNode? child = null)
    {
        Name = name;
        Child = child;
    }
}