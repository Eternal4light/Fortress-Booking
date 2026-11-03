namespace PartnerService.WebHost.Models
{
    public class PartnerResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string? Specialization { get; set; }

        public bool IsActive { get; set; }

        public int Capacity { get; set; }

    }
}
