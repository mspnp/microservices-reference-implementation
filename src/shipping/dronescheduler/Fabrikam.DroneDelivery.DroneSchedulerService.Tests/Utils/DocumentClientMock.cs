// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Moq;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.Tests.Utils
{
    public static class DocumentClientMock
    {
        public static IDocumentClient CreateDocumentClientMockObject<T>(
            IQueryable<T> fakeResults)
        {
            // doc query
            var fakeResponse = new FeedResponse<T>(fakeResults);

            var mockDocumentQuery = new Mock<IFakeDocumentQuery<T>>();

            var docProvider = new Mock<IQueryProvider>();
            docProvider
                .Setup(p => p.CreateQuery<T>(
                    It.IsAny<Expression>()))
                .Returns(mockDocumentQuery.Object);

            mockDocumentQuery
                .SetupSequence(q => q.HasMoreResults)
                .Returns(true)
                .Returns(false);

            mockDocumentQuery
                .Setup(q =>
                    q.ExecuteNextAsync<T>(
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeResponse);

            mockDocumentQuery
                .Setup(q => q.Provider)
                .Returns(docProvider.Object);
            mockDocumentQuery
                .Setup(q => q.ElementType)
                .Returns(fakeResults.ElementType);
            mockDocumentQuery
                .Setup(q => q.Expression)
                .Returns(fakeResults.Expression);
            mockDocumentQuery
                .Setup(q => q.GetEnumerator())
                .Returns(fakeResults.GetEnumerator());

            // db query
            var mockDatabaseQuery = new Mock<IOrderedQueryable<Database>>();
            var dbProvider = new Mock<IQueryProvider>();
            dbProvider
                .Setup(p => p.CreateQuery<Database>(
                    It.IsAny<Expression>()))
                .Returns(mockDatabaseQuery.Object);

            var fakeDbResutls = new List<Database>
                {
                    Mock.Of<Database>()
                }.AsQueryable();

            mockDatabaseQuery
                .Setup(q => q.Provider)
                .Returns(dbProvider.Object);
            mockDatabaseQuery
                .Setup(q => q.ElementType)
                .Returns(fakeDbResutls.ElementType);
            mockDatabaseQuery
                .Setup(q => q.Expression)
                .Returns(fakeDbResutls.Expression);
            mockDatabaseQuery
                .Setup(q => q.GetEnumerator())
                .Returns(fakeDbResutls.GetEnumerator());

            // collection query
            var mockCollectionQuery = new Mock<IOrderedQueryable<DocumentCollection>>();
            var colProvider = new Mock<IQueryProvider>();
            colProvider
                .Setup(p => p.CreateQuery<DocumentCollection>(
                    It.IsAny<Expression>()))
                .Returns(mockCollectionQuery.Object);

            var fakeColResutls = new List<DocumentCollection>
                {
                    Mock.Of<DocumentCollection>()
                }.AsQueryable();

            mockCollectionQuery
                .Setup(q => q.Provider)
                .Returns(colProvider.Object);
            mockCollectionQuery
                .Setup(q => q.ElementType)
                .Returns(fakeColResutls.ElementType);
            mockCollectionQuery
                .Setup(q => q.Expression)
                .Returns(fakeColResutls.Expression);
            mockCollectionQuery
                .Setup(q => q.GetEnumerator())
                .Returns(fakeColResutls.GetEnumerator());

            // DocumentClient mock
            var clientMock = new Mock<IDocumentClient>();

            clientMock
                .Setup(q => q.CreateDocumentQuery<T>(
                    It.IsAny<Uri>(),
                    It.IsAny<FeedOptions>()))
                .Returns(mockDocumentQuery.Object);

            clientMock
                .Setup(q => q.CreateDatabaseQuery(null))
                .Returns(mockDatabaseQuery.Object);

            clientMock
                .Setup(q => q.CreateDocumentCollectionQuery(It.IsAny<string>(), null))
                .Returns(mockCollectionQuery.Object);

            clientMock
                .Setup(q => q.ConnectionPolicy)
                .Returns(new ConnectionPolicy { ConnectionMode = ConnectionMode.Gateway });

            return clientMock.Object;
        }
    }
}
