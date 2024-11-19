namespace Alpha_API.Temp
{
    public class GymPackage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; } // Duration of the package in days
    }

}
