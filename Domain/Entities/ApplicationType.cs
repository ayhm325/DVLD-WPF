



namespace Domain.Entities
{
    public class ApplicationType
    {

        public int ApplicationTypeId { get; set; }
        public string ApplicationTypeTitle { get; set; } = null!;
        public decimal ApplicationFees { get; set; }
    }
}
