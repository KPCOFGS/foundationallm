﻿using Azure.Core;
using FoundationaLLM.Client.Management.Interfaces;
using FoundationaLLM.Common.Models.ResourceProviders;
using FoundationaLLM.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FoundationaLLM.Client.Management.Clients.Rest
{
    internal class ResourceRESTClient(
        IHttpClientFactory httpClientFactory,
        TokenCredential credential,
        string instanceId)
        : ManagementRESTClientBase(httpClientFactory, credential), IResourceRESTClient
    {
        private readonly string _instanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
        private readonly JsonSerializerOptions _jsonSerializerOptions = CommonJsonSerializerOptions.GetJsonSerializerOptions();

        /// <inheritdoc/>
        public async Task<T> GetResourcesAsync<T>(string resourceProvider, string resourcePath)
        {
            var managementClient = await GetManagementClientAsync();
            var response = await managementClient.GetAsync($"instances/{_instanceId}/providers/{resourceProvider}/{resourcePath}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(responseContent, _jsonSerializerOptions)!;
            }

            throw new Exception($"Failed to retrieve resources. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}");
        }

        /// <inheritdoc/>
        public async Task<ResourceProviderUpsertResult> UpsertResourceAsync(string resourceProvider, string resourcePath, object serializedResource)
        {
            var managementClient = await GetManagementClientAsync();
            var content = new StringContent(JsonSerializer.Serialize(serializedResource), Encoding.UTF8, "application/json");
            var response = await managementClient.PostAsync($"instances/{_instanceId}/providers/{resourceProvider}/{resourcePath}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ResourceProviderUpsertResult>(responseContent, _jsonSerializerOptions)!;
            }

            throw new Exception($"Failed to upsert resource. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}");
        }

        /// <inheritdoc/>
        public async Task DeleteResourceAsync(string resourceProvider, string resourcePath)
        {
            var managementClient = await GetManagementClientAsync();
            var response = await managementClient.DeleteAsync($"instances/{_instanceId}/providers/{resourceProvider}/{resourcePath}");

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            throw new Exception($"Failed to delete resource. Status code: {response.StatusCode}. Reason: {response.ReasonPhrase}");
        }
    }
}
