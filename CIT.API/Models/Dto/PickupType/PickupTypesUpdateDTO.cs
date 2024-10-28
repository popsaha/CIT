using System.ComponentModel.DataAnnotations;

namespace CIT.API.Models.Dto.PickupType
{
    public class PickupTypesUpdateDTO
    {
        [Required]
        public int PickupTypeId { get; set; }
        [Required]
        [MaxLength(30)]
        public string PickupTypeName { get; set; }
    }
}
