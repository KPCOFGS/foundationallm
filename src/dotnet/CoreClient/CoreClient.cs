﻿using FoundationaLLM.Client.Core.Interfaces;
using FoundationaLLM.Common.Models.Chat;
using FoundationaLLM.Common.Models.Orchestration;
using FoundationaLLM.Common.Models.ResourceProviders;
using FoundationaLLM.Common.Models.ResourceProviders.Agent;

namespace FoundationaLLM.Client.Core
{
    /// <summary>
    /// Provides high-level methods to interact with the Core API.
    /// </summary>
    public class CoreClient(ICoreRESTClient coreRestClient) : ICoreClient
    {
        /// <inheritdoc/>
        public async Task<string> CreateChatSessionAsync(string? sessionName, string token)
        {
            var sessionId = await coreRestClient.Sessions.CreateSessionAsync(token);
            if (!string.IsNullOrWhiteSpace(sessionName))
            {
                await coreRestClient.Sessions.RenameChatSession(sessionId, sessionName, token);
            }

            return sessionId;
        }

        /// <inheritdoc/>
        public async Task<Completion> SendCompletionWithSessionAsync(string? sessionId, string? sessionName,
            string userPrompt, string agentName, string token)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = await CreateChatSessionAsync(sessionName, token);
            }

            var orchestrationRequest = new OrchestrationRequest
            {
                AgentName = agentName,
                SessionId = sessionId,
                UserPrompt = userPrompt
            };
            return await SendCompletionWithSessionAsync(orchestrationRequest, token);
        }

        /// <inheritdoc/>
        public async Task<Completion> SendCompletionWithSessionAsync(OrchestrationRequest orchestrationRequest, string token)
        {
            if (string.IsNullOrWhiteSpace(orchestrationRequest.SessionId) ||
                string.IsNullOrWhiteSpace(orchestrationRequest.AgentName) ||
                string.IsNullOrWhiteSpace(orchestrationRequest.UserPrompt))
            {
                throw new ArgumentException("The orchestration request must contain a SessionID, AgentName, and UserPrompt at a minimum.");
            }

            var completion = await coreRestClient.Sessions.SendSessionCompletionRequestAsync(orchestrationRequest, token);
            return completion;
        }

        /// <inheritdoc/>
        public async Task<Completion> SendSessionlessCompletionAsync(string userPrompt, string agentName, string token)
        {
            var completionRequest = new CompletionRequest
            {
                AgentName = agentName,
                UserPrompt = userPrompt
            };

            return await SendSessionlessCompletionAsync(completionRequest, token);
        }

        /// <inheritdoc/>
        public async Task<Completion> SendSessionlessCompletionAsync(CompletionRequest completionRequest, string token)
        {
            if (string.IsNullOrWhiteSpace(completionRequest.AgentName) ||
                string.IsNullOrWhiteSpace(completionRequest.UserPrompt))
            {
                throw new ArgumentException("The completion request must contain an AgentName and UserPrompt at a minimum.");
            }

            var completion = await coreRestClient.Orchestration.SendOrchestrationCompletionRequestAsync(completionRequest, token);
            return completion;
        }

        /// <inheritdoc/>
        public async Task<Completion> AttachFileAndAskQuestionAsync(Stream fileStream, string fileName, string contentType,
            string agentName, string question, bool useSession, string? sessionId, string? sessionName, string token)
        {
            var objectId = await coreRestClient.Attachments.UploadAttachmentAsync(fileStream, fileName, contentType, token);

            if (useSession)
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    sessionId = await CreateChatSessionAsync(sessionName, token);
                }

                var orchestrationRequest = new OrchestrationRequest
                {
                    AgentName = agentName,
                    SessionId = sessionId,
                    UserPrompt = question,
                    Attachments = [objectId]
                };
                var sessionCompletion = await coreRestClient.Sessions.SendSessionCompletionRequestAsync(orchestrationRequest, token);

                return sessionCompletion;
            }

            // Use the orchestrated completion request to ask a question about the file.
            var completionRequest = new CompletionRequest
            {
                AgentName = agentName,
                UserPrompt = question,
                Attachments = [objectId]
            };
            var completion = await coreRestClient.Orchestration.SendOrchestrationCompletionRequestAsync(completionRequest, token);

            return completion;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Message>> GetChatSessionMessagesAsync(string sessionId, string token) => await coreRestClient.Sessions.GetChatSessionMessagesAsync(sessionId, token);

        /// <inheritdoc/>
        public async Task<IEnumerable<ResourceProviderGetResult<AgentBase>>> GetAgentsAsync(string token)
        {
            var agents = await coreRestClient.Orchestration.GetAgentsAsync(token);
            return agents;
        }

        /// <inheritdoc/>
        public async Task DeleteSessionAsync(string sessionId, string token) => await coreRestClient.Sessions.DeleteSessionAsync(sessionId, token);
    }
}
