﻿namespace CIT.API.Models.Dto.User
{
    public class LocalUserCreateDTO
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string RoleName { get; set; }
        public string RegionName { get; set; }
    }
}
