﻿using System.Text;
using System.Text.Json;
using FoundationaLLM.Common.Constants;
using FoundationaLLM.Common.Constants.Agents;
using FoundationaLLM.Common.Constants.ResourceProviders;
using FoundationaLLM.Common.Exceptions;
using FoundationaLLM.Common.Extensions;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Infrastructure;
using FoundationaLLM.Common.Models.Orchestration;
using FoundationaLLM.Common.Models.Orchestration.Direct;
using FoundationaLLM.Common.Models.ResourceProviders.AIModel;
using FoundationaLLM.Common.Models.ResourceProviders.Prompt;
using FoundationaLLM.Common.Models.ResourceProviders.AIModel;
using FoundationaLLM.Common.Settings;
using FoundationaLLM.Orchestration.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FoundationaLLM.Common.Models.ResourceProviders.Agent;
using FoundationaLLM.Common.Constants.Authentication;

namespace FoundationaLLM.Orchestration.Core.Services
{
    /// <summary>
    /// The Azure OpenAI direct orchestration service.
    /// </summary>
    /// <param name="logger">The logger used for logging.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> used to retrieve app settings from configuration.</param>
    /// <param name="httpClientFactoryService">The HTTP client factory service.</param>
    /// <param name="resourceProviderServices">A dictionary of <see cref="IResourceProviderService"/> resource providers hashed by resource provider name.</param>
    public class AzureOpenAIDirectService(
        ICallContext callContext,
        ILogger<AzureOpenAIDirectService> logger,
        IConfiguration configuration,
        IHttpClientFactoryService httpClientFactoryService,
        IEnumerable<IResourceProviderService> resourceProviderServices) : IAzureOpenAIDirectService
    {
        private readonly ICallContext _callContext = callContext;
        private readonly ILogger<AzureOpenAIDirectService> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        private readonly IHttpClientFactoryService _httpClientFactoryService = httpClientFactoryService;
        private readonly JsonSerializerOptions _jsonSerializerOptions = CommonJsonSerializerOptions.GetJsonSerializerOptions();
        private readonly Dictionary<string, IResourceProviderService> _resourceProviderServices = resourceProviderServices.ToDictionary(
                rps => rps.Name);

        /// <inheritdoc/>
        public async Task<ServiceStatusInfo> GetStatus() =>
            await Task.FromResult(new ServiceStatusInfo
            {
                Name = Name,
                Status = "ready",
            });

        /// <inheritdoc/>
        public string Name => LLMOrchestrationServiceNames.AzureOpenAIDirect;

        /// <inheritdoc/>
        public async Task<LLMCompletionResponse> GetCompletion(LLMCompletionRequest request)
        {
            var agent = request.Agent
                ?? throw new Exception("Agent cannot be null.");

            var endpointConfiguration = (agent.OrchestrationSettings?.AIModel?.Endpoint)
                ?? throw new Exception("Endpoint must be provided.");
            var deployment = request?.Settings?.AIModel?.DeploymentName ?? throw new Exception("Deployment name must be set on the AIModel");

            //var endpointSettings = GetEndpointSettings(endpointConfiguration);

            var inputStrings = new List<CompletionMessage>();
            SystemCompletionMessage? systemPrompt = null;

            if (endpointConfiguration.OperationType == OperationTypes.Chat)
            {
                if (!string.IsNullOrWhiteSpace(agent.PromptObjectId))
                {
                    if (!_resourceProviderServices.TryGetValue(ResourceProviderNames.FoundationaLLM_Prompt, out var promptResourceProvider))
                        throw new ResourceProviderException($"The resource provider {ResourceProviderNames.FoundationaLLM_Prompt} was not loaded.");

                    var prompt = await promptResourceProvider.GetResource<PromptBase>(agent.PromptObjectId, _callContext.CurrentUserIdentity!) as MultipartPrompt;

                    systemPrompt = new SystemCompletionMessage
                    {
                        Role = InputMessageRoles.System,
                        Content = prompt?.Prefix ?? string.Empty
                    };
                }

                // Add system prompt, if exists.
                if (systemPrompt != null) inputStrings.Add(systemPrompt);
                // Add conversation history.
                if (agent.ConversationHistory?.Enabled == true && request.MessageHistory != null)
                {
                    // The message history needs to be in a continuous order of user and assistant messages.
                    // If the MaxHistory value is odd, add one to the number of messages to take to ensure proper pairing.
                    if (agent.ConversationHistory.MaxHistory % 2 != 0)
                        agent.ConversationHistory.MaxHistory++;

                    var messageHistoryItems = request.MessageHistory?.TakeLast(agent.ConversationHistory.MaxHistory);
                    foreach (var item in messageHistoryItems!)
                    {
                        inputStrings.Add(new CompletionMessage
                        {
                            Role = item.Sender.ToLower(),
                            Content = item.Text
                        });
                    }
                }
                // Add current user prompt.
                var userPrompt = new UserCompletionMessage { Content = request.UserPrompt };
                inputStrings.Add(userPrompt);
            }

            if (!string.IsNullOrWhiteSpace(endpointConfiguration.Url) &&
                endpointConfiguration.AuthenticationParameters.TryGetValue(AuthenticationParameterKeys.APIKey, out var apiKeyVal))
              {
                var apiKey = apiKeyVal.ToString();
                var client = _httpClientFactoryService.CreateClient(HttpClients.AzureOpenAIDirect);
                if (endpointConfiguration.AuthenticationType == AuthenticationTypes.APIKey && !string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Add("api-key", apiKey);
                }

                client.BaseAddress = new Uri(endpointConfiguration.Url);

                var modelParameters = agent.OrchestrationSettings?.AIModel.ModelParameters;
                var modelOverrides = request.Settings?.ModelParameters;

                if (modelParameters != null)
                {
                    var azureOpenAIDirectRequest = modelParameters.ToObject<AzureOpenAICompletionRequest>(modelOverrides);
                    var chatOperation = string.Empty;

                    switch (endpointConfiguration.OperationType)
                    {
                        case OperationTypes.Completions:
                            azureOpenAIDirectRequest.Prompt = request.UserPrompt;
                            break;
                        case OperationTypes.Chat:
                            chatOperation = "/chat";
                            azureOpenAIDirectRequest.Messages = [.. inputStrings];
                            break;
                    }

                    var body = JsonSerializer.Serialize(azureOpenAIDirectRequest, _jsonSerializerOptions);
                    var content = new StringContent(body, Encoding.UTF8, "application/json");

                    var responseMessage = await client.PostAsync($"/openai/deployments/{deployment}{chatOperation}/completions?api-version={endpointConfiguration.APIVersion}", content);
                    var responseContent = await responseMessage.Content.ReadAsStringAsync();

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        var completionResponse = JsonSerializer.Deserialize<AzureOpenAICompletionResponse>(responseContent);

                        return new LLMCompletionResponse
                        {
                            Completion = !string.IsNullOrEmpty(endpointConfiguration.OperationType) && endpointConfiguration.OperationType == OperationTypes.Chat
                                ? completionResponse!.Choices?[0].Message?.Content
                                : completionResponse!.Choices?[0].Text,
                            UserPrompt = request.UserPrompt,
                            FullPrompt = body,
                            PromptTemplate = systemPrompt?.Content,
                            AgentName = agent.Name,
                            PromptTokens = completionResponse!.Usage!.PromptTokens,
                            CompletionTokens = completionResponse!.Usage!.CompletionTokens
                        };
                    }

                    _logger.LogWarning("The AzureOpenAIDirect orchestration service returned status code {StatusCode}: {ResponseContent}",
                        responseMessage.StatusCode, responseContent);
                }

            }

            return new LLMCompletionResponse
            {
                Completion = "A problem on my side prevented me from responding.",
                UserPrompt = request.UserPrompt,
                PromptTemplate = systemPrompt?.Content,
                AgentName = agent.Name,
                PromptTokens = 0,
                CompletionTokens = 0
            };
        }
    }
}
