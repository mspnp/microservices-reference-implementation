// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

export enum MongoErrors {
    CommandNotFound = 59,
    ShardKeyNotFound = 61,
    DuplicateKey = 11000,
    TooManyRequests = 16500 // See: https://docs.microsoft.com/en-us/azure/cosmos-db/faq
}
