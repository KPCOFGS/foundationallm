﻿using System.Text.Json.Serialization;

namespace FoundationaLLM.Common.Models.ResourceProviders.Attachment
{
    /// <summary>
    /// Attachment resource.
    /// </summary>
    public class AgentPrivateFile : ResourceBase
    {
        /// <summary>
        /// The type of the resource.
        /// </summary>
        [JsonIgnore]
        public override string? Type { get; set; } = nameof(AgentPrivateFile);

        /// <summary>
        /// A list of tools (object IDs) that are associated with the file.
        /// </summary>
        [JsonPropertyName("tool_object_ids")]
        public List<string> ToolObjectIds { get; set; } = new List<string>();

        /// <summary>
        /// File stream of the attachment contents.
        /// </summary>
        [JsonPropertyName("content")]
        public byte[]? Content { get; set; }

        /// <summary>
        /// The mime content type of the attachment.
        /// </summary>
        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }
    }
}
