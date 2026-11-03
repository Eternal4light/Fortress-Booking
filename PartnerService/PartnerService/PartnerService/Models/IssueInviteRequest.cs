namespace PartnerService.WebHost.Models
{
    public class IssueInviteRequest
    {
        public Guid ActivityId { get; set; }

        public string? Description { get; set; }
    }
}
