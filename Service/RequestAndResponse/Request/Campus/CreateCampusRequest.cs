using System.ComponentModel.DataAnnotations;

namespace Service.RequestAndResponse.Request.Campus
{
    public class CreateCampusRequest
    {
        [Required]
        [StringLength(100)]
        public string CampusName { get; set; }

        [StringLength(500)]
        public string Address { get; set; }
    }
}