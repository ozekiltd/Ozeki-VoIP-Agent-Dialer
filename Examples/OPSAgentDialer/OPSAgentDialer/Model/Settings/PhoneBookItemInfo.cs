using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OPSSDKCommon.Model;

namespace OPSAgentDialer.Model.Settings
{
    public class PhoneBookItemInfo
    {
        public PhoneBookItemInfo()
        {
            
        }

        public PhoneBookItemInfo(PhoneBookItem item)
        {
            Username = item.Username;
            Name = item.Name;
            PhoneNumber = item.PhoneNumber;
            Extensions = item.Extensions;
            Email = item.Email;
            PictureUrl = item.PictureUrl;
        }

        public string Username { get; set; }

        public string Name { get; set; }

        public List<string> Extensions { get; set; }

        public string PhoneNumber { get; set; }

        public string Email { get; set; }

        public string PictureUrl { get; set; }
    }
}
