namespace App.Domain.Model
{
  public class Connection
  {
    public int Id { get; set; }
    public int EdgeId { get; set; }
    public int FirstNodeId { get; set; }
    public int SecondNodeId { get; set; }

  }
}
