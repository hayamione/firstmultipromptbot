// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyMultiTurnBot
{
    public class UserProfile
    {
        
        //public string Name { get; set; }

        //public string Email { get; set; }

        //public int Phone { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public long Phone { get; set; }
        public long Salary { get; set; }

        // public Attachment Picture { get; set; }
    }
}
