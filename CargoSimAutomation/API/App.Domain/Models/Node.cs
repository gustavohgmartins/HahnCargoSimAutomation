namespace App.Domain.Model
{
  public class Node
  {
    public int Id { get; set; }
    public string Name { get; set; }

    public Node Clone()
    {
      return new Node
      {
        Id = this.Id,
        Name = this.Name
      };
    }

  }
}
