// import type { AccountEntity } from "@azure/msal-browser";

interface ResourceBase {
	object_id: string;
	type: string;
	name: string;
	display_name: string;
	description: string;
	properties?: { [key: string]: string | null };
	cost_center: string;
	expiration_date: string;
}

export type ResourceProviderGetResult<T> = {
	/**
	 * Represents the result of a fetch operation.
	 */
	resource: T;

	/**
	 * List of authorized actions on the resource.
	 */
	actions: string[];

	/**
	 * List of roles on the resource.
	 */
	roles: string[];
};

export type ResourceProviderUpsertResult = {
	/**
	 * The id of the object that was created or updated.
	 */
	objectId: string;

	/**
	 * A flag denoting whether the upserted resource already exists.
	 */
	resourceExists: boolean;

	/**
	 * Gets or sets the resource resulting from the upsert operation.
	 * Each resource provider will decide whether to return the resource in the upsert result or not.
	 */
	resource?: any;
};


export interface AgentWorkflowBase {
	/**
	 * The workflow resource associated with the agent.
	 */
	type?: string;

	/**
	 * The workflow resource associated with the agent.
	 */
	workflow_object_id: string;

	/**
	 * The name of the workflow resource associated with the agent.
	 */
	workflow_name: string;

	/**
	 * The collection of AI models available to the workflow.
	 * The well-known key "main-model" is used to specify the model for the main workflow.
	 */
	agent_workflow_ai_models: { [key: string]: AgentWorkflowAIModel };

	/**
	 * The collection of prompt resources available to the workflow.
	 * The well-known key "main-prompt" is used to specify the prompt for the main workflow.
	 */
	prompt_object_ids: { [key: string]: string };
}

export interface AgentWorkflowAIModel {
	/**
	 * The AI model object ID.
	 */
	ai_model_object_id: string;

	/**
	 * Dictionary with override values for the model parameters.
	 * For the list of supported keys, see ModelParametersKeys.
	 */
	model_parameters?: { [key: string]: any };
}

export type AgentTool = {
	name: string;
	description: string;
	ai_model_object_ids: { [key: string]: string };
	api_endpoint_configuration_object_ids: { [key: string]: string };
	properties: { [key: string]: any };
};

export type Agent = ResourceBase & {
	type: 'knowledge-management' | 'analytics';
	inline_context: boolean;

	ai_model_object_id: string;

	vectorization: {
		dedicated_pipeline: boolean;
		indexing_profile_object_ids: string[];
		text_embedding_profile_object_id: string;
		text_partitioning_profile_object_id: string;
		data_source_object_id: string;
		vectorization_data_pipeline_object_id: string;
		trigger_type: string;
		trigger_cron_schedule: string;
	};

	capabilities: string[];
	tools: AgentTool[];

	sessions_enabled: boolean;
	orchestration_settings: {
		orchestrator: string;
	};
	conversation_history_settings: {
		enabled: boolean;
		max_history: number;
	};
	gatekeeper_settings: {
		use_system_setting: boolean;
		options: string[];
	};
	language_model: {
		type: string;
		provider: string;
		temperature: number;
		use_chat: boolean;
		api_endpoint: string;
		api_key: string;
		api_version: string;
		version: string;
		deployment: string;
	};
	prompt_object_id: string;
	workflow: AgentWorkflowBase | null;
};

export type AgentAccessToken = ResourceBase & {
	id: string;
	active: boolean;
};

export type Prompt = ResourceBase & {
	object_id: string;
	description: string;
	prefix: string;
	suffix: string;
};

export type AgentDataSource = ResourceBase & {
	content_source: string;
	object_id: string;
};

export type ExternalOrchestrationService = ResourceBase & {
	category: string;
	api_url_configuration_name: string;
	api_key_configuration_name: string;
	url: string;
	status_url?: string | null;
	// The resolved value of the API key configuration reference for displaying in the UI and updating the configuration.
	resolved_api_key: string;
};

export type AIModel = ResourceBase & {
	// The object id of the APIEndpointConfiguration object providing the configuration for the API endpoint used to interact with the model.
	endpoint_object_id: string;
	// The version of the AI model.
	version?: string | null;
	// The name of the deployment corresponding to the AI model.
	deployment_name?: string | null;
	// Dictionary with default values for the model parameters.
	model_parameters: { [key: string]: any };
};

export interface ConfigurationReferenceMetadata {
	isKeyVaultBacked: boolean;
}

// Data sources
interface BaseDataSource extends ResourceBase {
	configuration_references: { [key: string]: string };
	// The resolved configuration references are used to store the resolved values for displaying in the UI and updating the configuration.
	resolved_configuration_references: { [key: string]: string | null };
	// The metadata objects that specify the nature of each configuration reference.
	configuration_reference_metadata: { [key: string]: ConfigurationReferenceMetadata };
}

export interface AzureDataLakeDataSource extends BaseDataSource {
	type: 'azure-data-lake';
	folders?: string[];
	configuration_references: {
		AuthenticationType: string;
		ConnectionString: string;
		APIKey: string;
		Endpoint: string;
		AccountName: string;
	};
}

export interface OneLakeDataSource extends BaseDataSource {
	type: 'onelake';
	workspaces?: string[];
	configuration_references: {
		AuthenticationType: string;
		ConnectionString: string;
		APIKey: string;
		Endpoint: string;
		AccountName: string;
	};
}

export interface AzureSQLDatabaseDataSource extends BaseDataSource {
	type: 'azure-sql-database';
	tables?: string[];
	configuration_references: {
		ConnectionString: string;
	};
}

export interface SharePointOnlineSiteDataSource extends BaseDataSource {
	type: 'sharepoint-online-site';
	site_url?: string;
	document_libraries?: string[];
	configuration_references: {
		ClientId: string;
		TenantId: string;
		CertificateName: string;
		KeyVaultURL: string;
	};
}

export type DataSource =
	| AzureDataLakeDataSource
	| SharePointOnlineSiteDataSource
	| OneLakeDataSource
	| AzureSQLDatabaseDataSource;
// End data sources

// App Configuration
export interface AppConfigBase extends ResourceBase {
	key: string;
	value: string;
	content_type: string | null;
}

export interface AppConfig extends AppConfigBase {
	type: 'appconfiguration-key-value';
	content_type: '';
}

export interface AppConfigKeyVault extends AppConfigBase {
	type: 'appconfiguration-key-vault-reference';
	key_vault_uri: string;
	key_vault_secret_name: string;
	content_type: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8';
}

export type AppConfigUnion = AppConfig | AppConfigKeyVault;
// End App Configuration

export type AgentIndex = ResourceBase & {
	indexer: string;
	settings: {
		IndexName: string;
		TopN?: string;
		Filters?: string;
		EmbeddingFieldName?: string;
		TextFieldName?: string;
	};
	configuration_references: {
		APIKey: string;
		AuthenticationType: string;
		Endpoint: string;
	};
	resolved_configuration_references: {
		APIKey: string;
		AuthenticationType: string;
		Endpoint: string;
	};
};

export type TextPartitioningProfile = ResourceBase & {
	text_splitter: string;
	settings: {
		Tokenizer: string;
		TokenizerEncoder: string;
		ChunkSizeTokens: string;
		OverlapSizeTokens: string;
	};
};

export type TextEmbeddingProfile = ResourceBase & {
	text_embedding: string;
	configuration_references: {
		APIKey: string;
		APIVersion: string;
		AuthenticationType: string;
		DeploymentName: string;
		Endpoint: string;
	};
	settings: {
		model_name: string;
	};
	// The resolved configuration references are used to store the resolved values for displaying in the UI and updating the configuration.
	resolved_configuration_references: {
		APIKey: string;
		APIVersion: string;
		AuthenticationType: string;
		DeploymentName: string;
		Endpoint: string;
	};
	resolved_settings: {
		model_name: string;
	};
};

export type CheckNameResponse = {
	type: string;
	name: string;
	status: string;
	message: string;
};

export type FilterRequest = {
	default?: boolean;
};

export type AgentGatekeeper = {};

export type MockCreateAgentRequest = {
	type: 'knowledge' | 'analytics';
	storageSource: number;
	indexSource: number;
	processing: {
		chunkSize: number;
		overlapSize: number;
	};
	trigger: {
		frequency: 'auto' | 'manual' | 'scheduled';
	};
	conversation_history: {
		enabled: boolean;
		max_history: number;
	};
	gatekeeper: {
		use_system_setting: boolean;
		options: {
			content_safety: number;
			data_protection: number;
		};
	};
	prompt: string;
};

export type CreateAgentRequest = ResourceBase & {
	type: 'knowledge-management' | 'analytics';
	inline_context: boolean;

	ai_model_object_id: string;

	language_model: {
		type: string;
		provider: string;
		temperature: number;
		use_chat: boolean;
		api_endpoint: string;
		api_key: string;
		api_version: string;
		version: string;
		deployment: string;
	};

	capabilities: string[];
	tools: AgentTool[];

	vectorization: {
		dedicated_pipeline: boolean;
		indexing_profile_object_ids: string[];
		text_embedding_profile_object_id: string;
		text_partitioning_profile_object_id: string;
		data_source_object_id: string;
		vectorization_data_pipeline_object_id: string;
		trigger_type: string;
		trigger_cron_schedule: string;
	};

	sessions_enabled: boolean;
	orchestration_settings: {
		orchestrator: string;
	};
	conversation_history_settings: {
		enabled: boolean;
		max_history: number;
	};
	gatekeeper_settings: {
		use_system_setting: boolean;
		options: string[];
	};
	prompt_object_id: string;
};

export type CreatePromptRequest = ResourceBase & {
	type: 'basic' | 'multipart';
	prefix: string;
	suffix: string;
};

export type CreateTextPartitioningProfileRequest = ResourceBase & {
	text_splitter: string;
	settings: {
		Tokenizer: string;
		TokenizerEncoder: string;
		ChunkSizeTokens: string;
		OverlapSizeTokens: string;
	};
};

export type Role = {};

export type RoleAssignment = {};

// Type guards
export function isAzureDataLakeDataSource(
	dataSource: DataSource,
): dataSource is AzureDataLakeDataSource {
	return dataSource.type === 'azure-data-lake';
}

export function isOneLakeDataSource(dataSource: DataSource): dataSource is OneLakeDataSource {
	return dataSource.type === 'onelake';
}

export function isSharePointOnlineSiteDataSource(
	dataSource: DataSource,
): dataSource is SharePointOnlineSiteDataSource {
	return dataSource.type === 'sharepoint-online-site';
}

export function isAzureSQLDatabaseDataSource(
	dataSource: DataSource,
): dataSource is AzureSQLDatabaseDataSource {
	return dataSource.type === 'azure-sql-database';
}

export function isAppConfig(config: AppConfigUnion): config is AppConfig {
	return config.type === 'appconfiguration-key-value';
}

export function isAppConfigKeyVault(config: AppConfigUnion): config is AppConfigKeyVault {
	return config.type === 'appconfiguration-key-vault-reference';
}

export function convertDataSourceToAzureDataLake(dataSource: DataSource): AzureDataLakeDataSource {
	return {
		type: 'azure-data-lake',
		name: dataSource.name,
		object_id: dataSource.object_id,
		cost_center: dataSource.cost_center,
		description: dataSource.description,
		folders: dataSource.folders || [],
		configuration_references: {
			AuthenticationType: dataSource.configuration_references?.AuthenticationType || '',
			ConnectionString: dataSource.configuration_references?.ConnectionString || '',
			APIKey: dataSource.configuration_references?.APIKey || '',
			Endpoint: dataSource.configuration_references?.Endpoint || '',
			AccountName: dataSource.configuration_references?.AccountName || '',
		},
		configuration_reference_metadata: {
			AuthenticationType: { isKeyVaultBacked: false },
			ConnectionString: { isKeyVaultBacked: true },
			APIKey: { isKeyVaultBacked: true },
			Endpoint: { isKeyVaultBacked: false },
			AccountName: { isKeyVaultBacked: false },
		},
		resolved_configuration_references: dataSource.resolved_configuration_references,
	};
}

export function convertDataSourceToOneLake(dataSource: DataSource): OneLakeDataSource {
	return {
		type: 'onelake',
		name: dataSource.name,
		object_id: dataSource.object_id,
		cost_center: dataSource.cost_center,
		description: dataSource.description,
		workspaces: dataSource.workspaces || [],
		configuration_references: {
			AuthenticationType: dataSource.configuration_references?.AuthenticationType || '',
			ConnectionString: dataSource.configuration_references?.ConnectionString || '',
			APIKey: dataSource.configuration_references?.APIKey || '',
			Endpoint: dataSource.configuration_references?.Endpoint || '',
			AccountName: dataSource.configuration_references?.AccountName || '',
		},
		configuration_reference_metadata: {
			AuthenticationType: { isKeyVaultBacked: false },
			ConnectionString: { isKeyVaultBacked: true },
			APIKey: { isKeyVaultBacked: true },
			Endpoint: { isKeyVaultBacked: false },
			AccountName: { isKeyVaultBacked: false },
		},
		resolved_configuration_references: dataSource.resolved_configuration_references,
	};
}

export function convertDataSourceToSharePointOnlineSite(
	dataSource: DataSource,
): SharePointOnlineSiteDataSource {
	return {
		type: 'sharepoint-online-site',
		name: dataSource.name,
		object_id: dataSource.object_id,
		cost_center: dataSource.cost_center,
		description: dataSource.description,
		site_url: dataSource.site_url || '',
		document_libraries: dataSource.document_libraries || [],
		configuration_references: {
			ClientId: dataSource.configuration_references?.ClientId || '',
			TenantId: dataSource.configuration_references?.TenantId || '',
			CertificateName: dataSource.configuration_references?.CertificateName || '',
			KeyVaultURL: dataSource.configuration_references?.KeyVaultURL || '',
		},
		configuration_reference_metadata: {
			ClientId: { isKeyVaultBacked: false },
			TenantId: { isKeyVaultBacked: false },
			CertificateName: { isKeyVaultBacked: false },
			KeyVaultURL: { isKeyVaultBacked: false },
		},
		resolved_configuration_references: dataSource.resolved_configuration_references,
	};
}

export function convertDataSourceToAzureSQLDatabase(
	dataSource: DataSource,
): AzureSQLDatabaseDataSource {
	return {
		type: 'azure-sql-database',
		name: dataSource.name,
		object_id: dataSource.object_id,
		cost_center: dataSource.cost_center,
		description: dataSource.description,
		tables: dataSource.tables || [],
		configuration_references: {
			ConnectionString: dataSource.configuration_references?.ConnectionString || '',
		},
		configuration_reference_metadata: {
			ConnectionString: { isKeyVaultBacked: true },
		},
		resolved_configuration_references: dataSource.resolved_configuration_references,
	};
}

export function convertToDataSource(dataSource: DataSource): DataSource {
	if (isAzureDataLakeDataSource(dataSource)) {
		return convertDataSourceToAzureDataLake(dataSource);
	} else if (isOneLakeDataSource(dataSource)) {
		return convertDataSourceToOneLake(dataSource);
	} else if (isSharePointOnlineSiteDataSource(dataSource)) {
		return convertDataSourceToSharePointOnlineSite(dataSource);
	} else if (isAzureSQLDatabaseDataSource(dataSource)) {
		return convertDataSourceToAzureSQLDatabase(dataSource);
	}
	return dataSource;
}

export function convertToAppConfig(baseConfig: AppConfigUnion): AppConfig {
	return {
		...baseConfig,
		type: 'appconfiguration-key-value',
		content_type: '',
	};
}

export function convertToAppConfigKeyVault(baseConfig: AppConfigUnion): AppConfigKeyVault {
	if (!('key_vault_uri' in baseConfig) || !('key_vault_secret_name' in baseConfig)) {
		throw new Error('Missing Key Vault properties');
	}

	return {
		...baseConfig,
		type: 'appconfiguration-key-vault-reference',
		key_vault_uri: baseConfig.key_vault_uri,
		key_vault_secret_name: baseConfig.key_vault_secret_name,
		content_type: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8',
	};
}
