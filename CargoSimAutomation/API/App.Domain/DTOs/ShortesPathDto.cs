namespace App.Domain.DTOs
{
    public class ShortestPathDto
    {
        public Dictionary<int, int> PreviousNodes { get; set; }
        public TimeSpan TotalCost { get; set; }
    }
}
