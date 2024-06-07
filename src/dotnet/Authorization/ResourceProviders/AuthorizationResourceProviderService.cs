﻿using FluentValidation;
using FoundationaLLM.Authorization.Models;
using FoundationaLLM.Common.Constants.Authorization;
using FoundationaLLM.Common.Constants.ResourceProviders;
using FoundationaLLM.Common.Exceptions;
using FoundationaLLM.Common.Extensions;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Authentication;
using FoundationaLLM.Common.Models.Authorization;
using FoundationaLLM.Common.Models.Collections;
using FoundationaLLM.Common.Models.Configuration.Instance;
using FoundationaLLM.Common.Models.ResourceProviders;
using FoundationaLLM.Common.Services.ResourceProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FoundationaLLM.Authorization.ResourceProviders
{
    /// <summary>
    /// Implements the FoundationaLLM.Authorization resource provider.
    /// </summary>
    /// <param name="instanceOptions">The options providing the <see cref="InstanceSettings"/> with instance settings.</param>
    /// <param name="authorizationService">The <see cref="IAuthorizationService"/> providing authorization services.</param>
    /// <param name="resourceValidatorFactory">The <see cref="IResourceValidatorFactory"/> providing the factory to create resource validators.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> of the main dependency injection container.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used to provide loggers for logging.</param>
    /// <param name="accountService">The <see cref="IAccountService"/>.</param>
    public class AuthorizationResourceProviderService(
        IOptions<InstanceSettings> instanceOptions,
        IAuthorizationService authorizationService,
        IResourceValidatorFactory resourceValidatorFactory,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        IAccountService accountService)
        : ResourceProviderServiceBase(
            instanceOptions.Value,
            authorizationService,
            null,
            null,
            resourceValidatorFactory,
            serviceProvider,
            loggerFactory.CreateLogger<AuthorizationResourceProviderService>(),
            [])
    {
        private readonly IAccountService _accountService = accountService;

        protected override Dictionary<string, ResourceTypeDescriptor> GetResourceTypes() =>
            AuthorizationResourceProviderMetadata.AllowedResourceTypes;

        /// <inheritdoc/>
        protected override string _name => ResourceProviderNames.FoundationaLLM_Authorization;

        /// <inheritdoc/>
        protected override async Task InitializeInternal() =>
            await Task.CompletedTask;

        #region Support for Management API

        /// <inheritdoc/>
        protected override async Task<object> GetResourcesAsync(ResourcePath resourcePath, UnifiedUserIdentity userIdentity) =>
            resourcePath.ResourceTypeInstances[0].ResourceType switch
            {
                AuthorizationResourceTypeNames.RoleAssignments => await LoadRoleAssignments(resourcePath.ResourceTypeInstances[0], userIdentity),
                AuthorizationResourceTypeNames.RoleDefinitions => LoadRoleDefinitions(resourcePath.ResourceTypeInstances[0], userIdentity),
                AuthorizationResourceTypeNames.AuthorizableActions => LoadAuthorizableActions(resourcePath.ResourceTypeInstances[0], userIdentity),
                _ => throw new ResourceProviderException($"The resource type {resourcePath.ResourceTypeInstances[0].ResourceType} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest)
            };

        #region Helpers for GetResourcesAsyncInternal

        private static List<RoleDefinition> LoadRoleDefinitions(ResourceTypeInstance instance, UnifiedUserIdentity userIdentity)
        {
            if (instance.ResourceId == null)
                return RoleDefinitions.All.Values.ToList();
            else
            {
                if (RoleDefinitions.All.TryGetValue(instance.ResourceId, out var roleDefinition))
                    return [roleDefinition];
                else
                    return [];
            }
        }

        private static List<AuthorizableAction> LoadAuthorizableActions(ResourceTypeInstance instance, UnifiedUserIdentity userIdentity)
        {
            if (instance.ResourceId == null)
                return [.. AuthorizableActions.Actions.Values];
            else
            {
                if (AuthorizableActions.Actions.TryGetValue(instance.ResourceId, out var authorizableAction))
                    return [authorizableAction];
                else
                    return [];
            }
        }

        private async Task<List<ResourceProviderGetResult<RoleAssignment>>> LoadRoleAssignments(ResourceTypeInstance instance, UnifiedUserIdentity userIdentity)
        {
            var roleAssignments = await GetAllRoleAssignments();

            if (instance.ResourceId != null)
            {
                var roleAssignment = roleAssignments.Where(roleAssignment => roleAssignment.ObjectId == instance.ResourceId).SingleOrDefault();

                if (roleAssignment == null)
                    throw new ResourceProviderException($"Could not locate the {instance.ResourceId} role assignment resource.",
                        StatusCodes.Status404NotFound);
                else
                    roleAssignments = [roleAssignment];
            }

            return await _authorizationService.FilterResourcesByAuthorizableAction(
               _instanceSettings.Id, userIdentity, roleAssignments,
               AuthorizableActionNames.FoundationaLLM_Authorization_RoleAssignments_Read);
        }

        #endregion

        /// <inheritdoc/>
        protected override async Task<object> UpsertResourceAsync(ResourcePath resourcePath, string serializedResource, UnifiedUserIdentity userIdentity) =>
            resourcePath.ResourceTypeInstances[0].ResourceType switch
            {
                AuthorizationResourceTypeNames.RoleAssignments => await UpdateRoleAssignments(resourcePath, serializedResource, userIdentity),
                _ => throw new ResourceProviderException($"The resource type {resourcePath.ResourceTypeInstances[0].ResourceType} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest)
            };

        #region Helpers for UpsertResourceAsync

        private async Task<ResourceProviderUpsertResult> UpdateRoleAssignments(ResourcePath resourcePath, string serializedRoleAssignment, UnifiedUserIdentity userIdentity)
        {
            var roleAssignment = JsonSerializer.Deserialize<RoleAssignment>(serializedRoleAssignment)
                ?? throw new ResourceProviderException("The object definition is invalid.",
                    StatusCodes.Status400BadRequest);

            if (resourcePath.ResourceTypeInstances[0].ResourceId != roleAssignment.Name)
                throw new ResourceProviderException("The resource path does not match the object definition (name mismatch).",
                    StatusCodes.Status400BadRequest);

            roleAssignment.ObjectId = resourcePath.GetObjectId(_instanceSettings.Id, _name);

            var roleAssignmentValidator = _resourceValidatorFactory.GetValidator<RoleAssignment>()!;
            var context = new ValidationContext<object>(roleAssignment);
            var validationResult = await roleAssignmentValidator.ValidateAsync(context);
            if (!validationResult.IsValid)
            {
                throw new ResourceProviderException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                    StatusCodes.Status400BadRequest);
            }

            var roleAssignmentResult = await _authorizationService.ProcessRoleAssignmentRequest(
                _instanceSettings.Id,
                new RoleAssignmentRequest()
                {
                    Name = roleAssignment.Name,
                    Description = roleAssignment.Description,
                    ObjectId = roleAssignment.ObjectId,
                    PrincipalId = roleAssignment.PrincipalId,
                    PrincipalType = roleAssignment.PrincipalType,
                    RoleDefinitionId = roleAssignment.RoleDefinitionId,
                    Scope = roleAssignment.Scope
                });

            if (roleAssignmentResult.Success)
                return new ResourceProviderUpsertResult
                {
                    ObjectId = roleAssignment.ObjectId
                };

            throw new ResourceProviderException("The role assignment failed.");
        }

        #endregion

        /// <inheritdoc/>
        protected override async Task DeleteResourceAsync(ResourcePath resourcePath, UnifiedUserIdentity userIdentity)
        {
            switch (resourcePath.ResourceTypeInstances.Last().ResourceType)
            {
                case AuthorizationResourceTypeNames.RoleAssignments:
                    await _authorizationService.RevokeRole(_instanceSettings.Id, resourcePath.ResourceTypeInstances.Last().ResourceId!);
                    break;
                default:
                    throw new ResourceProviderException($"The resource type {resourcePath.ResourceTypeInstances.Last().ResourceType} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest);
            };
        }

        #endregion

        /// <inheritdoc/>
        protected override async Task<object> ExecuteActionAsync(ResourcePath resourcePath, string serializedAction, UnifiedUserIdentity userIdentity) =>
            resourcePath.ResourceTypeInstances.Last().ResourceType switch
            {
                AuthorizationResourceTypeNames.Accounts => resourcePath.ResourceTypeInstances.Last().Action switch
                {
                    AuthorizationResourceProviderActions.GetUsers => await LoadUserAccounts(serializedAction),
                    AuthorizationResourceProviderActions.GetGroups => await LoadGroupAccounts(serializedAction),
                    AuthorizationResourceProviderActions.GetObjects => await LoadAccounts(serializedAction),
                    _ => throw new ResourceProviderException($"The action {resourcePath.ResourceTypeInstances.Last().Action} is not supported by the {_name} resource provider.",
                        StatusCodes.Status400BadRequest)
                },
                AuthorizationResourceTypeNames.RoleAssignments => resourcePath.ResourceTypeInstances.Last().Action switch
                {
                    AuthorizationResourceProviderActions.Filter => await FilterRoleAssignments(resourcePath.ResourceTypeInstances[0], serializedAction, userIdentity),
                    _ => throw new ResourceProviderException($"The action {resourcePath.ResourceTypeInstances.Last().Action} is not supported by the {_name} resource provider.",
                        StatusCodes.Status400BadRequest)
                },
                _ => throw new ResourceProviderException()
            };

        #region Helpers for ExecuteActionAsync
        private async Task<List<ResourceProviderGetResult<RoleAssignment>>> FilterRoleAssignments(ResourceTypeInstance resourceTypeInstance, string serializedAction, UnifiedUserIdentity userIdentity)
        {
            var parameters = JsonSerializer.Deserialize<RoleAssignmentQueryParameters>(serializedAction)!;

            if (string.IsNullOrWhiteSpace(parameters.Scope))
                throw new ResourceProviderException();
            else
                return (await LoadRoleAssignments(resourceTypeInstance, userIdentity)).Where(x => x.Resource.Scope == parameters.Scope).ToList();
        }

        private async Task<List<ObjectQueryResult>> LoadAccounts(string serializedAction) =>
            await _accountService.GetObjectsByIdsAsync(JsonSerializer.Deserialize<ObjectQueryParameters>(serializedAction)!);

        private async Task<PagedResponse<UserAccount>> LoadUserAccounts(string serializedAction)
        {
            var parameters = JsonSerializer.Deserialize<AccountQueryParameters>(serializedAction)!;

            if (string.IsNullOrWhiteSpace(parameters.Id))
                return await _accountService.GetUsersAsync(parameters);
            else
            {
                var userAccount = await _accountService.GetUserByIdAsync(parameters.Id);
                return new PagedResponse<UserAccount>() { Items = [userAccount], TotalItems = 1, HasNextPage = false };
            }
        }

        private async Task<PagedResponse<GroupAccount>> LoadGroupAccounts(string serializedAction)
        {
            var parameters = JsonSerializer.Deserialize<AccountQueryParameters>(serializedAction)!;

            if (string.IsNullOrWhiteSpace(parameters.Id))
                return await _accountService.GetUserGroupsAsync(parameters);
            else
            {
                var userGroup = await _accountService.GetUserGroupByIdAsync(parameters.Id);
                return new PagedResponse<GroupAccount>() { Items = [userGroup], TotalItems = 1, HasNextPage = false };
            }
        }

        #endregion

        private async Task<List<RoleAssignment>> GetAllRoleAssignments()
        {
            var roleAssignments = new List<RoleAssignment>();
            var roleAssignmentObjects = await _authorizationService.GetRoleAssignments(_instanceSettings.Id);
            foreach (var obj in roleAssignmentObjects)
            {
                var roleAssignment = JsonSerializer.Deserialize<RoleAssignment>(obj.ToString()!)!;
                if (!roleAssignment.Deleted)
                    roleAssignments.Add(roleAssignment);
            }

            return roleAssignments;
        }
    }
}
