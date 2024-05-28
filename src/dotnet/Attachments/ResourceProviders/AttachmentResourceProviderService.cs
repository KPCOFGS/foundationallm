﻿using Azure.Messaging;
using FluentValidation;
using FoundationaLLM.Common.Constants;
using FoundationaLLM.Common.Constants.Authorization;
using FoundationaLLM.Common.Constants.Configuration;
using FoundationaLLM.Common.Constants.ResourceProviders;
using FoundationaLLM.Common.Exceptions;
using FoundationaLLM.Common.Extensions;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Authentication;
using FoundationaLLM.Common.Models.Configuration.Instance;
using FoundationaLLM.Common.Models.Events;
using FoundationaLLM.Common.Models.ResourceProviders;
using FoundationaLLM.Common.Models.ResourceProviders.Attachment;
using FoundationaLLM.Common.Services.ResourceProviders;
using FoundationaLLM.Attachment.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using FoundationaLLM.Common.Models.ResourceProviders.Prompt;

namespace FoundationaLLM.Attachment.ResourceProviders
{
    /// <summary>
    /// Implements the FoundationaLLM.Attachment resource provider.
    /// </summary>
    /// <param name="instanceOptions">The options providing the <see cref="InstanceSettings"/> with instance settings.</param>
    /// <param name="authorizationService">The <see cref="IAuthorizationService"/> providing authorization services.</param>
    /// <param name="storageService">The <see cref="IStorageService"/> providing storage services.</param>
    /// <param name="eventService">The <see cref="IEventService"/> providing event services.</param>
    /// <param name="resourceValidatorFactory">The <see cref="IResourceValidatorFactory"/> providing the factory to create resource validators.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> of the main dependency injection container.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used to provide loggers for logging.</param>
    public class AttachmentResourceProviderService(
        IOptions<InstanceSettings> instanceOptions,
        IAuthorizationService authorizationService,
        [FromKeyedServices(DependencyInjectionKeys.FoundationaLLM_ResourceProvider_Attachment)] IStorageService storageService,
        IEventService eventService,
        IResourceValidatorFactory resourceValidatorFactory,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
        : ResourceProviderServiceBase(
            instanceOptions.Value,
            authorizationService,
            storageService,
            eventService,
            resourceValidatorFactory,
            serviceProvider,
            loggerFactory.CreateLogger<AttachmentResourceProviderService>(),
            [
                EventSetEventNamespaces.FoundationaLLM_ResourceProvider_Attachment
            ])
    {
        /// <inheritdoc/>
        protected override Dictionary<string, ResourceTypeDescriptor> GetResourceTypes() =>
            AttachmentResourceProviderMetadata.AllowedResourceTypes;

        private ConcurrentDictionary<string, AttachmentReference> _attachmentReferences = [];

        /// <inheritdoc/>
        protected override string _name => ResourceProviderNames.FoundationaLLM_Attachment;
        private const string ATTACHMENT_REFERENCES_FILE_NAME = "_attachment-references.json";
        private const string ATTACHMENT_REFERENCES_FILE_PATH =
            $"/{ResourceProviderNames.FoundationaLLM_Attachment}/{ATTACHMENT_REFERENCES_FILE_NAME}";


        /// <inheritdoc/>
        protected override async Task InitializeInternal()
        {
            _logger.LogInformation("Starting to initialize the {ResourceProvider} resource provider...", _name);
            if (await _storageService.FileExistsAsync(_storageContainerName, ATTACHMENT_REFERENCES_FILE_PATH, default))
            {
                var fileContent = await _storageService.ReadFileAsync(
                    _storageContainerName,
                    ATTACHMENT_REFERENCES_FILE_PATH,
                    default);

                var resourceReferenceStore =
                    JsonSerializer.Deserialize<ResourceReferenceStore<AttachmentReference>>(
                        Encoding.UTF8.GetString(fileContent.ToArray()));

                _attachmentReferences = new ConcurrentDictionary<string, AttachmentReference>(
                        resourceReferenceStore!.ToDictionary());
            }
            else
            {
                await _storageService.WriteFileAsync(
                    _storageContainerName,
                    ATTACHMENT_REFERENCES_FILE_PATH,
                    JsonSerializer.Serialize(new ResourceReferenceStore<AttachmentReference>
                    {
                        ResourceReferences = []
                    }),
                    default,
                    default);
            }

            _logger.LogInformation("The {ResourceProvider} resource provider was successfully initialized.", _name);
        }

        #region Support for Management API

        /// <inheritdoc/>
        protected override async Task<object> GetResourcesAsync(ResourcePath resourcePath, UnifiedUserIdentity userIdentity) =>
            resourcePath.ResourceTypeInstances[0].ResourceType switch
            {
                AttachmentResourceTypeNames.Attachments => await LoadAttachments(resourcePath.ResourceTypeInstances[0], userIdentity),
                _ => throw new ResourceProviderException($"The resource type {resourcePath.ResourceTypeInstances[0].ResourceType} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest)
            };

        #region Helpers for GetResourcesAsyncInternal

        private async Task<List<ResourceProviderGetResult<AttachmentBase>>> LoadAttachments(ResourceTypeInstance instance, UnifiedUserIdentity userIdentity)
        {
            var attachments = new List<AttachmentBase>();

            if (instance.ResourceId == null)
            {
                attachments = (await Task.WhenAll(_attachmentReferences.Values
                                         .Where(dsr => !dsr.Deleted)
                                         .Select(dsr => LoadAttachment(dsr))))
                                             .Where(ds => ds != null)
                                             .Select(ds => ds!)
                                             .ToList();

            }
            else
            {
                AttachmentBase? attachment;
                if (!_attachmentReferences.TryGetValue(instance.ResourceId, out var AttachmentReference))
                {
                    attachment = await LoadAttachment(null, instance.ResourceId);
                    if (attachment != null)
                        attachments.Add(attachment);
                }
                else
                {
                    if (AttachmentReference.Deleted)
                        throw new ResourceProviderException(
                            $"Could not locate the {instance.ResourceId} attachment resource.",
                            StatusCodes.Status404NotFound);

                    attachment = await LoadAttachment(AttachmentReference);
                    if (attachment != null)
                        attachments.Add(attachment);
                }
            }
            return attachments.Select(attachment => new ResourceProviderGetResult<AttachmentBase>() { Resource = attachment, Actions = [], Roles = [] }).ToList();
        }

        private async Task<AttachmentBase?> LoadAttachment(AttachmentReference? AttachmentReference, string? resourceId = null)
        {
            if (AttachmentReference != null || !string.IsNullOrWhiteSpace(resourceId))
            {
                AttachmentReference ??= new AttachmentReference
                {
                    Name = resourceId!,
                    Type = AttachmentTypes.Basic,
                    Filename = $"/{_name}/{resourceId}.wav",
                    Deleted = false
                };
                if (await _storageService.FileExistsAsync(_storageContainerName, AttachmentReference.Filename, default))
                {
                    var fileContent = await _storageService.ReadFileAsync(_storageContainerName, AttachmentReference.Filename, default);
                    var attachment = JsonSerializer.Deserialize(
                               Encoding.UTF8.GetString(fileContent.ToArray()),
                               AttachmentReference.AttachmentType,
                               _serializerSettings) as AttachmentBase
                           ?? throw new ResourceProviderException($"Failed to load the attachment {AttachmentReference.Name}.",
                               StatusCodes.Status400BadRequest);

                    if (!string.IsNullOrWhiteSpace(resourceId))
                    {
                        AttachmentReference.Type = attachment.Type!;
                        _attachmentReferences.AddOrUpdate(AttachmentReference.Name, AttachmentReference, (k, v) => AttachmentReference);
                    }

                    return attachment;
                }

                if (string.IsNullOrWhiteSpace(resourceId))
                {
                    // Remove the reference from the dictionary since the file does not exist.
                    _attachmentReferences.TryRemove(AttachmentReference.Name, out _);
                    return null;
                }
            }
            throw new ResourceProviderException($"Could not locate the {AttachmentReference.Name} attachment resource.",
                StatusCodes.Status404NotFound);
        }

        #endregion


        protected override async Task UpsertResourceAsync<T>(ResourcePath resourcePath, T resource) 
        {

            //TODO: generalize for other attachment types
            var audioAttachment = resource as AudioAttachmentStreamed;
            if (audioAttachment == null)
                throw new ResourceProviderException($"Invalid resource type");

            if (resourcePath.ResourceTypeInstances[0].ResourceType != AttachmentResourceTypeNames.Attachments)
            {
                throw new ResourceProviderException(
                        $"The resource type {resourcePath.ResourceTypeInstances[0].ResourceType} is not supported by the {_name} resource provider.",
                        StatusCodes.Status400BadRequest);
            }
            await UpdateAttachment(resourcePath, audioAttachment);
        }



        /// <inheritdoc/>
        protected override Task<object> UpsertResourceAsync(ResourcePath resourcePath, string serializedResource, UnifiedUserIdentity userIdentity) => null;

        #region Helpers for UpsertResourceAsync

        private async Task<ResourceProviderUpsertResult> UpdateAttachment(ResourcePath resourcePath, AudioAttachmentStreamed attachment)
        {

            if (_attachmentReferences.TryGetValue(attachment.Name!, out var existingAttachmentReference)
                && existingAttachmentReference!.Deleted)
                throw new ResourceProviderException($"The attachment resource {existingAttachmentReference.Name} cannot be added or updated.",
                        StatusCodes.Status400BadRequest);

            if (resourcePath.ResourceTypeInstances[0].ResourceId != attachment.Name)
                throw new ResourceProviderException("The resource path does not match the object definition (name mismatch).",
                    StatusCodes.Status400BadRequest);

            var AttachmentReference = new AttachmentReference
            {
                Name = attachment.Name!,
                Type = attachment.Type!,
                Filename = $"/{_name}/{attachment.Name}.wav",
                Deleted = false
            };

            attachment.ObjectId = resourcePath.GetObjectId(_instanceSettings.Id, _name);

            var validator = _resourceValidatorFactory.GetValidator(AttachmentReference.AttachmentType);
            if (validator is IValidator attachmentValidator)
            {
                var context = new ValidationContext<object>(attachment);
                var validationResult = await attachmentValidator.ValidateAsync(context);
                if (!validationResult.IsValid)
                {
                    throw new ResourceProviderException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                        StatusCodes.Status400BadRequest);
                }
            }

            await _storageService.WriteFileAsync(
                _storageContainerName,
                AttachmentReference.Filename,
                JsonSerializer.Serialize<AttachmentBase>(attachment, _serializerSettings),
                default,
                default);

            _attachmentReferences.AddOrUpdate(AttachmentReference.Name, AttachmentReference, (k, v) => AttachmentReference);

            await _storageService.WriteFileAsync(
                    _storageContainerName,
                    ATTACHMENT_REFERENCES_FILE_PATH,
                    JsonSerializer.Serialize(ResourceReferenceStore<AttachmentReference>.FromDictionary(_attachmentReferences.ToDictionary())),
                    default,
                    default);

            return new ResourceProviderUpsertResult
            {
                ObjectId = (attachment as AttachmentBase)!.ObjectId
            };
        }

        #endregion

        /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected override async Task<object> ExecuteActionAsync(ResourcePath resourcePath, string serializedAction, UnifiedUserIdentity userIdentity) => throw new NotImplementedException();
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <inheritdoc/>
        protected override async Task DeleteResourceAsync(ResourcePath resourcePath, UnifiedUserIdentity userIdentity)
        {
            switch (resourcePath.ResourceTypeInstances.Last().ResourceType)
            {
                case AttachmentResourceTypeNames.Attachments:
                    await DeleteAttachment(resourcePath.ResourceTypeInstances);
                    break;
                default:
                    throw new ResourceProviderException($"The resource type {resourcePath.ResourceTypeInstances.Last().ResourceType} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest);
            };
        }

        #region Helpers for DeleteResourceAsync

        private async Task DeleteAttachment(List<ResourceTypeInstance> instances)
        {
            if (_attachmentReferences.TryGetValue(instances.Last().ResourceId!, out var AttachmentReference))
            {
                if (!AttachmentReference.Deleted)
                {
                    AttachmentReference.Deleted = true;

                    await _storageService.WriteFileAsync(
                        _storageContainerName,
                        ATTACHMENT_REFERENCES_FILE_PATH,
                        JsonSerializer.Serialize(ResourceReferenceStore<AttachmentReference>.FromDictionary(_attachmentReferences.ToDictionary())),
                        default,
                        default);
                }
            }
            else
            {
                throw new ResourceProviderException($"Could not locate the {instances.Last().ResourceId} attachment resource.",
                    StatusCodes.Status404NotFound);
            }
        }

        #endregion

        #endregion

        /// <inheritdoc/>
        protected override T GetResourceInternal<T>(ResourcePath resourcePath) where T : class
        {
            if (resourcePath.ResourceTypeInstances.Count != 1)
                throw new ResourceProviderException($"Invalid resource path");

            if (typeof(T) != typeof(AttachmentBase))
                throw new ResourceProviderException($"The type of requested resource ({typeof(T)}) does not match the resource type specified in the path ({resourcePath.ResourceTypeInstances[0].ResourceType}).");

            _attachmentReferences.TryGetValue(resourcePath.ResourceTypeInstances[0].ResourceId!, out var AttachmentReference);
            if (AttachmentReference == null || AttachmentReference.Deleted)
                throw new ResourceProviderException($"The resource {resourcePath.ResourceTypeInstances[0].ResourceId!} of type {resourcePath.ResourceTypeInstances[0].ResourceType} was not found.");

            var attachment = LoadAttachment(AttachmentReference).Result;
            return attachment as T
                ?? throw new ResourceProviderException($"The resource {resourcePath.ResourceTypeInstances[0].ResourceId!} of type {resourcePath.ResourceTypeInstances[0].ResourceType} was not found.");
        }


        #region Event handling

        /// <inheritdoc/>
        protected override async Task HandleEvents(EventSetEventArgs e)
        {
            _logger.LogInformation("{EventsCount} events received in the {EventsNamespace} events namespace.",
                e.Events.Count, e.Namespace);

            switch (e.Namespace)
            {
                case EventSetEventNamespaces.FoundationaLLM_ResourceProvider_Attachment:
                    foreach (var @event in e.Events)
                        await HandleAttachmentResourceProviderEvent(@event);
                    break;
                default:
                    // Ignore sliently any event namespace that's of no interest.
                    break;
            }

            await Task.CompletedTask;
        }

        private async Task HandleAttachmentResourceProviderEvent(CloudEvent e)
        {
            if (string.IsNullOrWhiteSpace(e.Subject))
                return;

            var fileName = e.Subject.Split("/").Last();

            _logger.LogInformation("The file [{FileName}] managed by the [{ResourceProvider}] resource provider has changed and will be reloaded.",
                fileName, _name);

            var AttachmentReference = new AttachmentReference
            {
                Name = Path.GetFileNameWithoutExtension(fileName),
                Filename = $"/{_name}/{fileName}",
                Type = AttachmentTypes.Basic,
                Deleted = false
            };

            var attachment = await LoadAttachment(AttachmentReference);
            AttachmentReference.Name = attachment.Name;
            AttachmentReference.Type = attachment.Type!;

            _attachmentReferences.AddOrUpdate(
                AttachmentReference.Name,
                AttachmentReference,
                (k, v) => v);

            _logger.LogInformation("The attachment reference for the [{AttachmentName}] agent or type [{AttachmentType}] was loaded.",
                AttachmentReference.Name, AttachmentReference.Type);
        }

        #endregion
    }
}
