﻿using System.Text.Json.Serialization;

namespace FoundationaLLM.Common.Models.ResourceProviders.Agent.AgentWorkflows
{
    /// <summary>
    /// Provides an agent workflow configuration for an OpenAI Assistants workflow.
    /// </summary>
    public class OpenAIAssistantsAgentWorkflow: AgentWorkflowBase
    {
        /// <inheritdoc/>
        [JsonIgnore]
        public override string Type => AgentWorkflowTypes.OpenAIAssistants;
                
        /// <summary>
        /// The OpenAI Assistant ID for the agent workflow.
        /// </summary>
        [JsonPropertyName("assistant_id")]
        public required string AssistantId { get; set; }
    }
}
