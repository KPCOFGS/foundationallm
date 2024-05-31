﻿namespace FoundationaLLM.Common.Models.Configuration.CosmosDB
{
    /// <summary>
    /// Stores the Azure Cosmos DB settings from the app configuration.
    /// </summary>
    public record CosmosDbSettings
    {
        /// <summary>
        /// The Azure Cosmos DB endpoint URL.
        /// </summary>
        public required string Endpoint { get; set; }
        /// <summary>
        /// The name of the Azure Cosmos DB database.
        /// </summary>
        public required string Database { get; set; }
        /// <summary>
        /// The name of the Azure Cosmos DB vector database.
        /// </summary>
        public string? VectorDatabase { get; set; }
        /// <summary>
        /// Comma-separated list of Azure Cosmos DB container names.
        /// </summary>
        public required string Containers { get; set; }
        /// <summary>
        /// Comma-separated list of Azure Cosmos DB container names to monitor for changes.
        /// </summary>
        public required string MonitoredContainers { get; set; }
        /// <summary>
        /// The name of the Azure Cosmos DB container used for change feed leases.
        /// </summary>
        public required string ChangeFeedLeaseContainer { get; set; }
        /// <summary>
        /// Specifies whether to enable Azure Cosmos DB tracing. Disabling tracing reduces
        /// the number of logs generated by the Azure Cosmos DB SDK.
        /// </summary>
        public bool EnableTracing { get; set; }
    }
}
