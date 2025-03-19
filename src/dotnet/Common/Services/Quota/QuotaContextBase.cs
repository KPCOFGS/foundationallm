﻿using FoundationaLLM.Common.Models.Quota;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace FoundationaLLM.Common.Services.Quota
{
    /// <summary>
    /// Implements the base class for managing the in-memory state of a quota context.
    /// </summary>
    /// <param name="quota">The <see cref="QuotaDefinition"/> providing the quota configuration.</param>
    /// <param name="logger">The logger used for logging.</param>
    public abstract class QuotaContextBase(
        QuotaDefinition quota,
        ILogger logger)
    {
        /// <summary>
        /// The object used to synchronize access to the quota context.
        /// </summary>
        protected readonly object _syncRoot = new();

        /// <summary>
        /// The logger used for logging.
        /// </summary>
        protected readonly ILogger _logger = logger;

        /// <summary>
        /// The <see cref="QuotaDefinition"/> providing the quota configuration.
        /// </summary>
        private readonly QuotaDefinition _quota = quota;

        /// <summary>
        /// Gets or sets the context.
        /// </summary>
        public QuotaDefinition Quota => _quota;

        /// <summary>
        /// Gets the quota metric partition that corresponds to the specified user identifier and/or user principal name.
        /// </summary>
        /// <param name="userIdentifier">The user identifier used to get the quota metric partition</param>
        /// <param name="userPrincipalName">The user principal name used to get the quota metric partition.</param>
        /// <returns>A <see cref="QuotaMetricPartition"/> instance.</returns>
        protected abstract QuotaMetricPartition GetQuotaMetricPartition(
            string userIdentifier,
            string userPrincipalName);

        /// <summary>
        /// Gets the quota metric partition that corresponds to the specified partition identifier.
        /// </summary>
        /// <param name="partitionId">The quota metric partition id.</param>
        /// <returns>A <see cref="QuotaMetricPartition"/> instance.</returns>
        protected abstract QuotaMetricPartition GetQuotaMetricPartition(
            string partitionId);

        /// <summary>
        /// Adds a new local unit of the quota metric to the quota context and checks if the quota is exceeded or not.
        /// </summary>
        /// <param name="referenceTime">The time at which the local metric unit was recorded.</param>
        /// <param name="userIdentifier">The user identifier associated with the unit of the quota metric.</param>
        /// <param name="userPrincipalName">The user principal name associated with the unit of the quota metric.</param>
        /// <returns>The result of the quota evaluation.</returns>
        public QuotaMetricPartitionState AddLocalMetricUnit(
            DateTimeOffset referenceTime,
            string userIdentifier,
            string userPrincipalName)
        {
            var startTime = DateTimeOffset.UtcNow;
            var metricPartition = GetQuotaMetricPartition(userIdentifier, userPrincipalName);
            var metricPartitionState = metricPartition.AddLocalMetricUnit(referenceTime);

            _logger.LogDebug(string.Join(Environment.NewLine,
                [
                    "Local metric unit added to quota context.",
                    "Quota context: {QuotaContext}, Reference time: {ReferenceTime}",
                    "Metric count: {MetricCount}, Quota exceeded: {QuotaExceeded}, Remaining lockout: {RemainingLockout} seconds.",
                    "Time elapsed: {TimeElapsedMilliseconds} ms",
                    "----------------------------------------"
                ]),
                $"{Quota.Context}, {userIdentifier}, {userPrincipalName}",
                referenceTime,
                $"{metricPartitionState.TotalMetricCount} (local = {metricPartitionState.LocalMetricCount}, remote = {metricPartitionState.RemoteMetricCount})",
                metricPartitionState.QuotaExceeded,
                metricPartitionState.TimeUntilRetrySeconds,
                (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);

            return metricPartitionState;
        }

        /// <summary>
        /// Adds remote units of the quota metric to the quota context.
        /// </summary>
        /// <param name="referenceTimes">The times at which the remote metric units were recorded.</param>
        /// <param name="partitionId">The quota metric partition identifier.</param>
        public void AddRemoteMetricUnits(
            List<DateTimeOffset> referenceTimes,
            string partitionId)
        {
            var startTime = DateTimeOffset.UtcNow;
            var metricPartition = GetQuotaMetricPartition(partitionId);
            var metricPartitionState = metricPartition.AddRemoteMetricUnits(referenceTimes);

            if (metricPartitionState != null)
                _logger.LogDebug(string.Join(Environment.NewLine,
                    [
                        "Remote metric units added to quota context.",
                        "Quota context: {QuotaContext}, Reference times: {ReferenceTimes}",
                        "Metric count: {MetricCount}, Quota exceeded: {QuotaExceeded}",
                        "Time elapsed: {TimeElapsedMilliseconds} ms",
                        "----------------------------------------"
                    ]),
                    Quota.Context,
                    referenceTimes,
                    $"{metricPartitionState.TotalMetricCount} (local = {metricPartitionState.LocalMetricCount}, remote = {metricPartitionState.RemoteMetricCount})",
                    metricPartitionState.QuotaExceeded,
                    (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
        }
    }
}
