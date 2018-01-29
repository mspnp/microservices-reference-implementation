// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.DroneDelivery.Common
{
    public class UserAccount
    {
        public UserAccount(string userid, string accountid)
        {
            UserId = userid;
            AccountId = accountid;
        }
        public string UserId { get; }
        public string AccountId { get; }
    }
}
