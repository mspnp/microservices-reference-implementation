// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { Package } from '../../app/models/package';
import { Repository } from '../../app/models/repository'
import { ObjectID } from 'mongodb';

var MongoClient = require('mongodb').MongoClient;

var assert = require('assert');

import 'mocha';

describe('Repository', function() {

    var connectionUrl = 'mongodb://packagedb:27017/local';
    var MongoClient = require('mongodb').MongoClient;
    var db;
    var repository = new Repository();

    before(async function() {
       await Repository.initialize(connectionUrl);

       db = await MongoClient.connect(connectionUrl)
       });

    afterEach(async function() {
        try {
            await db.collection(Repository.collectionName).drop();
        }
        catch (err) {

        }
    });

    it('addPackage', async function() {
        var package1 = new Package('1');
        package1.tag = "tag1";
        await repository.addPackage(package1);

        // Verify the package was added
        var package2  = (await repository.findPackage(package1._id));
        assert.ok(package1._id == package2._id);
        assert.equal(package2.tag, "tag1");
    });

    it('addDuplicatePackage', async function() {
        var package1 = new Package('1');
        package1.size = "small";
        await repository.addPackage(package1);

        package1.size = "large";
        await repository.addPackage(package1);
       
        // Verify the package was updated
        var package2  = (await repository.findPackage(package1._id));
        assert.ok(package1._id == package2._id);
        assert.equal(package2.size, "large");
    });

    it('updatePackage', async function() {
        var package1 = new Package('1');
        package1.tag = "tag1";
        await repository.addPackage(package1);

        package1.tag = "tag2";
        await repository.updatePackage(package1);

        var package2 = await repository.findPackage(package1._id);
        assert.ok(package1._id == package2._id);
        assert.equal(package2.tag, "tag2");
    });

    it('findPackage-returns-null-if-not-found', async function() {
        var package1 = await repository.findPackage('1');
        assert.equal(null, package1);
    });
});

