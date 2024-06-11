﻿using System.Net.Http.Headers;
using FoundationaLLM.Common.Models.Chat;
using FoundationaLLM.Common.Models.ResourceProviders;
using FoundationaLLM.Common.Models.ResourceProviders.Agent;

namespace FoundationaLLM.Client.Core.Interfaces
{
    /// <summary>
    /// Provides high-level methods to interact with the Core API.
    /// </summary>
    public interface ICoreClient
    {
        /// <summary>
        /// Creates a new chat session and renames it if a session name is provided.
        /// </summary>
        /// <param name="sessionName">Renames the new chat session if not null or empty.</param>
        /// <param name="token">The authentication token to send with the request.</param>
        /// <returns>The new chat session ID.</returns>
        Task<string> CreateChatSessionAsync(string? sessionName, string token);

        /// <summary>
        /// Runs a single completion request with an agent using the Core API and a chat session.
        /// To specify an existing session, provide the session ID. Otherwise, a new session will
        /// be created. If the session name is provided, the session is renamed.
        /// </summary>
        /// <param name="sessionId">The ID of an existing session. If null or empty, a new session
        /// is created first.</param>
        /// <param name="sessionName">Renames the new chat session if not null or empty.</param>
        /// <param name="userPrompt">The user prompt to send to the agent.</param>
        /// <param name="agentName">The name of the FoundationaLLM agent that will handle the
        /// completion request.</param>
        /// <param name="token">The authentication token to send with the request.</param>
        /// <returns>A completion from the designated FoundationaLLM agent.</returns>
        Task<Completion> SendCompletionWithSessionAsync(string? sessionId, string? sessionName,
            string userPrompt, string agentName, string token);

        /// <summary>
        /// Runs a single completion with an agent using the Core API without a chat session
        /// (sessionless). This method sends a user prompt to the agent and returns the completion
        /// response.
        /// </summary>
        /// <param name="userPrompt">The user prompt to send to the agent.</param>
        /// <param name="agentName">The name of the FoundationaLLM agent that will handle the
        /// completion request.</param>
        /// <param name="token"></param>
        /// <returns>A completion from the designated FoundationaLLM agent.</returns>
        Task<Completion> SendSessionlessCompletionAsync(string userPrompt, string agentName, string token);

        /// <summary>
        /// Attaches a file to the completion request and sends a question to the agent using the
        /// Core API.If the useSession parameter is true, the method uses an existing session or
        /// creates a new chat session and optionally renames it.
        /// existing session
        /// </summary>
        /// <param name="fileStream">The file contents of the new Attachment resource.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="contentType">The Content-Type header value of a valid mime type that is used
        /// to create a new <see cref="MediaTypeHeaderValue"/> as part of the
        /// <see cref="MultipartFormDataContent"/> sent to the API endpoint.</param>
        /// <param name="agentName">The name of the FoundationaLLM agent that will handle the
        /// completion request.</param>
        /// <param name="question">The user prompt to send to the agent along with the attachment.</param>
        /// <param name="useSession">If true, the completion is sent to a new or existing session. If
        /// false, no session is created and the sessionless orchestration flow is used.</param>
        /// <param name="sessionId">The ID of an existing session. If null or empty, a new session
        /// is created first.</param>
        /// <param name="sessionName">Renames the new chat session if not null or empty.</param>
        /// <param name="userPrompt">The user prompt to send to the agent.</param>
        /// <param name="agentName">The name of the FoundationaLLM agent that will handle the
        /// completion request.</param>
        /// <param name="token">The authentication token to send with the request.</param>
        /// <returns>A completion from the designated FoundationaLLM agent.</returns>
        /// <returns>A completion from the designated FoundationaLLM agent.</returns>
        Task<Completion> AttachFileAndAskQuestionAsync(Stream fileStream, string fileName, string contentType,
            string agentName, string question, bool useSession, string? sessionId, string? sessionName, string token);

        /// <summary>
        /// Retrieves agents available to the user for orchestration and session-based requests.
        /// </summary>
        /// <param name="token">The authentication token to send with the request.</param>
        /// <returns>A list of available agents.</returns>
        Task<IEnumerable<ResourceProviderGetResult<AgentBase>>> GetAgentsAsync(string token);

        /// <summary>
        /// Deletes a chat session.
        /// </summary>
        /// <param name="sessionId">The ID of the session to delete.</param>
        /// <param name="token">The authentication token to send with the request.</param>
        /// <returns></returns>
        Task DeleteSessionAsync(string sessionId, string token);
    }
}
