﻿namespace CIT.API.Models.Dto.User
{
    public class LocalUserDTO
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string RoleName { get; set; }
        public string RegionName { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool? IsActive { get; set; }
    }
}
