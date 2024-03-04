namespace App.Domain.Model
{
  public class Grid
  {
    public List<Node>? Nodes { get; set; }
    public List<Edge>? Edges { get; set; }
    public List<Connection>? Connections { get; set; }
  }
}
