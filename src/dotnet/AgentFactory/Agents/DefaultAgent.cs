﻿using FoundationaLLM.AgentFactory.Core.Interfaces;
using FoundationaLLM.AgentFactory.Core.Models.Messages;
using FoundationaLLM.AgentFactory.Core.Models.Orchestration.DataSourceConfigurations;
using FoundationaLLM.AgentFactory.Core.Models.Orchestration.Metadata;
using FoundationaLLM.AgentFactory.Core.Models.Orchestration;
using FoundationaLLM.AgentFactory.Interfaces;
using FoundationaLLM.Common.Models.Orchestration;

namespace FoundationaLLM.AgentFactory.Core.Agents
{
    /// <summary>
    /// DefaultAgent class
    /// </summary>
    public class DefaultAgent : AgentBase
    {
        LLMOrchestrationCompletionRequest _completionRequestTemplate = null!;

        /// <summary>
        /// Constructor for default agent.
        /// </summary>
        /// <param name="agentMetadata"></param>
        /// <param name="orchestrationService"></param>
        /// <param name="promptHubService"></param>
        /// <param name="dataSourceHubService"></param>
        public DefaultAgent(
            AgentMetadata agentMetadata,
            ILLMOrchestrationService orchestrationService,
            IPromptHubAPIService promptHubService,
            IDataSourceHubAPIService dataSourceHubService)
            : base(agentMetadata, orchestrationService, promptHubService, dataSourceHubService)
        {
        }

        /// <summary>
        /// used to configure the DeafultAgent class.
        /// </summary>
        /// <param name="userPrompt"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async override Task Configure(string userPrompt)
        {
            //get prompts for the agent from the prompt hub
            var promptResponse = await _promptHubService.ResolveRequest(_agentMetadata.Name!);

            //get data sources listed for the agent           
            var dataSourceResponse = await _dataSourceHubService.ResolveRequest(_agentMetadata.AllowedDataSourceNames!);

            MetadataBase dataSourceMetadata = null!;

            var dataSource = dataSourceResponse.DataSources![0];

            switch (_agentMetadata.Type)
            {
                case "csv":
                case "generic-resolver":                   
                case "blob-storage":
                    dataSourceMetadata = new BlobStorageDataSource
                    {
                        Name = dataSource.Name,
                        Type = _agentMetadata.Type,
                        Description = dataSource.Description,
                        Configuration = new BlobStorageConfiguration
                        {
                            ConnectionStringSecretName = dataSource.Authentication!["connection_string_secret"],
                            ContainerName = dataSource.Container,
                            Files = dataSource.Files
                        },
                        DataDescription = dataSource.DataDescription
                    };
                    break;              
                    
                case "search-service":
                    break;                
                case "anomaly":
                case "sql":
                    dataSourceMetadata = new SQLDatabaseDataSource
                    {
                        Name = dataSource.Name,
                        Type = _agentMetadata.Type,
                        Description = dataSource.Description,
                        Configuration = new SQLDatabaseConfiguration
                        {
                            Dialect = dataSource.Dialect,
                            Host = dataSource.Authentication!["host"],
                            Port = Convert.ToInt32(dataSource.Authentication["port"]),
                            DatabaseName = dataSource.Authentication["database"],
                            Username = dataSource.Authentication["username"],
                            PasswordSecretSettingKeyName = dataSource.Authentication["password_secret"],
                            IncludeTables = dataSource.IncludeTables!,
                            ExcludeTables = dataSource.ExcludeTables!,
                            RowLevelSecurityEnabled = dataSource.RowLevelSecurityEnabled ?? false,
                            FewShotExampleCount = dataSource.FewShotExampleCount ?? 0
                        },
                        DataDescription = dataSource.DataDescription
                    };
                    break;
                default:
                    throw new ArgumentException($"The {_agentMetadata.Type} data source type is not supported.");
            }

            //create LLMOrchestrationCompletionRequest template
            _completionRequestTemplate = new LLMOrchestrationCompletionRequest()
            {
                UserPrompt = null, // to be filled in GetCompletion / GetSummary
                Agent = new Agent()
                {
                    Name = _agentMetadata.Name,
                    Type = _agentMetadata.Type,
                    Description = _agentMetadata.Description,
                    PromptPrefix = promptResponse.Prompt?.PromptPrefix,
                    PromptSuffix = promptResponse.Prompt?.PromptSuffix
                },
                LanguageModel = new LanguageModel()
                {
                    Type = _agentMetadata.LanguageModel!.ModelType,
                    Provider = _agentMetadata.LanguageModel.Provider,
                    Temperature = _agentMetadata.LanguageModel.Temperature ?? 0f,
                    UseChat = _agentMetadata.LanguageModel.UseChat ?? true
                },
                DataSourceMetadata = dataSourceMetadata,
                MessageHistory = null // to be filled in GetCompletion
            };
        }

        /// <summary>
        /// Calls the orchestration service for the agent to get a completion
        /// </summary>
        /// <param name="completionRequest"></param>
        /// <returns></returns>
        public async override Task<CompletionResponse> GetCompletion(CompletionRequest completionRequest)
        {
            _completionRequestTemplate.UserPrompt = completionRequest.UserPrompt;
            _completionRequestTemplate.MessageHistory = completionRequest.MessageHistory;

            var result = await _orchestrationService.GetCompletion(_completionRequestTemplate);

            return new CompletionResponse()
            {
                Completion = result.Completion!,
                UserPrompt = completionRequest.UserPrompt,
                PromptTemplate = result.PromptTemplate,
                AgentName = result.AgentName,
                PromptTokens = result.PromptTokens,
                CompletionTokens = result.CompletionTokens,
            };
        }

        /// <summary>
        /// Calls the orchestration service for the agent to get a summary
        /// </summary>
        /// <param name="summaryRequest"></param>
        /// <returns></returns>
        public async override Task<SummaryResponse> GetSummary(SummaryRequest summaryRequest)
        {
            var summary = await _orchestrationService.GetSummary(summaryRequest.UserPrompt);

            return new SummaryResponse
            {
                Summary = summary
            };
        }
    }
}
