﻿using Azure;
using FoundationaLLM.Common.Constants;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Gatekeeper.Core.Interfaces;
using FoundationaLLM.Gatekeeper.Core.Models.ConfigurationOptions;
using FoundationaLLM.Gatekeeper.Core.Models.ContentSafety;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace FoundationaLLM.Gatekeeper.Core.Services
{
    /// <summary>
    /// Implements the <see cref="IContentSafetyService"/> interface.
    /// </summary>
    public class AzureContentSafetyService : IContentSafetyService
    {
        private readonly IOrchestrationContext _callContext;
        private readonly IHttpClientFactoryService _httpClientFactoryService;
        private readonly AzureContentSafetySettings _settings;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor for the Azure Content Safety service.
        /// </summary>
        /// <param name="callContext">Stores context information extracted from the current HTTP request. This information
        /// is primarily used to inject HTTP headers into downstream HTTP calls.</param>
        /// <param name="httpClientFactoryService">The HTTP client factory service.</param>
        /// <param name="options">The configuration options for the Azure Content Safety service.</param>
        /// <param name="logger">The logger for the Azure Content Safety service.</param>
        public AzureContentSafetyService(
            IOrchestrationContext callContext,
            IHttpClientFactoryService httpClientFactoryService,
            IOptions<AzureContentSafetySettings> options,
            ILogger<AzureContentSafetyService> logger)
        {
            _callContext = callContext;
            _httpClientFactoryService = httpClientFactoryService;
            _settings = options.Value;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<AnalyzeTextFilterResult> AnalyzeText(string content)
        {
            var client = await _httpClientFactoryService.CreateClient(HttpClientNames.AzureContentSafety, _callContext.CurrentUserIdentity);

            AnalyzeTextResult? results = null;
            try
            {
                var response = await client.PostAsync("/contentsafety/text:analyze?api-version=2023-10-01",
                new StringContent(JsonSerializer.Serialize(new { text = content }),
                Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    results = JsonSerializer.Deserialize<AnalyzeTextResult>(responseContent);
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"Analyze prompt text failed with status code: {ex.Status}, error code: {ex.ErrorCode}, message: {ex.Message}");
                results = null;
            }

            if (results == null)
                return new AnalyzeTextFilterResult { Safe = false, Reason = "The content safety service was unable to validate the prompt text due to an internal error." };

            var safe = true;
            var reason = "The prompt text did not pass the content safety filter. Reason:";

            var hateSeverity = results.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Hate)?.Severity ?? 0;
            if (hateSeverity > _settings.HateSeverity)
            {
                reason += $" hate";
                safe = false;
            }

            var violenceSeverity = results.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Violence)?.Severity ?? 0;
            if (violenceSeverity > _settings.ViolenceSeverity)
            {
                reason += $" violence";
                safe = false;
            }

            var selfHarmSeverity = results.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.SelfHarm)?.Severity ?? 0;
            if (selfHarmSeverity > _settings.SelfHarmSeverity)
            {
                reason += $" self-harm";
                safe = false;
            }

            var sexualSeverity = results.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Sexual)?.Severity ?? 0;
            if (sexualSeverity > _settings.SexualSeverity)
            {
                reason += $" sexual";
                safe = false;
            }

            return new AnalyzeTextFilterResult() { Safe = safe, Reason = safe ? string.Empty : reason };
        }

        /// <inheritdoc/>
        public async Task<string?> DetectPromptInjection(string content)
        {
            var client = await _httpClientFactoryService.CreateClient(HttpClientNames.AzureContentSafety, _callContext.CurrentUserIdentity);

            var response = await client.PostAsync("/contentsafety/text:shieldPrompt?api-version=2024-02-15-preview",
                new StringContent(JsonSerializer.Serialize(new
                {
                    userPrompt = content,
                    documents = new List<string>()
                }),
                Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<PromptShieldResult>(responseContent);

                if (results!.UserPromptAnalysis.AttackDetected)
                {
                    return "The prompt text did not pass the safety filter. Reason: Prompt injection or jailbreak detected.";
                }
            }

            return null;
        }
    }
}
