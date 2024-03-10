namespace App.Domain.DTOs
{
    public class BestPathDto
    {
        public Dictionary<int, int> PreviousNodes { get; set; }
        
        public BestPathParamsDto Params { get; set; }
    }
}
