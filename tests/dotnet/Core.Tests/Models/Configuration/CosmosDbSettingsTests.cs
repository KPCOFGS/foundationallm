﻿using FoundationaLLM.Common.Models.Configuration.CosmosDB;

namespace FoundationaLLM.Core.Tests.Models.Configuration
{
    public class CosmosDbSettingsTests
    {
        [Fact]
        public static void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            string expectedEndpoint = "endpoint";
            string expectedDatabase = "database";
            string expectedContainers = "containers";
            string expectedMonitoredContainers = "monitoredContainers";
            string expectedChangeFeedLeaseContainer = "changeFeedLeaseContainer";
            bool expectedEnableTracing = false;

            // Act
            var cosmosDbSettings = CreateCosmosDbSettings(
                expectedEndpoint,
                expectedDatabase,
                expectedContainers,
                expectedMonitoredContainers,
                expectedChangeFeedLeaseContainer,
                expectedEnableTracing
            );

            // Assert
            Assert.Equal(expectedEndpoint, cosmosDbSettings.Endpoint);
            Assert.Equal(expectedDatabase, cosmosDbSettings.Database);
            Assert.Equal(expectedContainers, cosmosDbSettings.Containers);
            Assert.Equal(expectedMonitoredContainers, cosmosDbSettings.MonitoredContainers);
            Assert.Equal(expectedChangeFeedLeaseContainer, cosmosDbSettings.ChangeFeedLeaseContainer);
            Assert.Equal(expectedEnableTracing, cosmosDbSettings.EnableTracing);
        }

        private static AzureCosmosDBSettings CreateCosmosDbSettings(string endpoint, string database, string containers, 
            string monitoredContainers, string changeFeedLeaseContainer, bool enableTracing)
        {
            return new AzureCosmosDBSettings() 
            {
                Endpoint = endpoint,
                Database = database,
                Containers = containers,
                MonitoredContainers = monitoredContainers,
                ChangeFeedLeaseContainer = changeFeedLeaseContainer,
                EnableTracing = enableTracing
            };
        }
    }
}
