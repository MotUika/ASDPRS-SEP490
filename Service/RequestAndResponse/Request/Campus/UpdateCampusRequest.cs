using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Campus
{
    public class UpdateCampusRequest
    {
        [Required]
        public int CampusId { get; set; }

        [StringLength(100)]
        public string CampusName { get; set; }

        [StringLength(500)]
        public string Address { get; set; }
    }
}