// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { ObjectID } from 'mongodb';

export type PackageSize = "small" | "medium" | "large";

export class Package {
    readonly _id: ObjectID;
    tag: string;
    weight: number;
    size: PackageSize;

    constructor(id? : string) {
        this._id = id || (new ObjectID(id)).toHexString();
    }
}