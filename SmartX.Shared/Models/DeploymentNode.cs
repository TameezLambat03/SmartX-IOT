namespace SmartX.Shared.Models;

public class DeploymentNode
{
    public string Name { get; set; }

    public List<DeploymentNode> Children { get; set; }

    public DeploymentNode(
        string name,
        IEnumerable<DeploymentNode>? children = null)
    {
        Name = name;
        Children = children?.ToList()
            ?? new List<DeploymentNode>();
    }

    public void AddChild(DeploymentNode child)
    {
        Children.Add(child);
    }
}