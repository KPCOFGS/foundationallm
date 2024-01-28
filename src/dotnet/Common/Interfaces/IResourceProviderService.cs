﻿using FoundationaLLM.Common.Models.ResourceProvider;

namespace FoundationaLLM.Common.Interfaces
{
    /// <summary>
    /// Provides the core services implemented by all resource providers. 
    /// </summary>
    public interface IResourceProviderService
    {
        /// <summary>
        /// The name of the resource provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Indicates whether the resource provider is initialized or not.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes the resource provider.
        /// </summary>
        /// <returns></returns>
        Task Initialize();

        /// <summary>
        /// Gets the resources based on the logical path of the resource type.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="resourcePath">The logical path of the resource type.</param>
        /// <returns>The <see cref="IList{T}"/> of resources corresponding to the specified logical path.</returns>
        Task<IList<T>> GetResourcesAsync<T>(string resourcePath) where T : class;

        /// <summary>
        /// Gets the resources based on the logical path of the resource type.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="resourcePath">The logical path of the resource type.</param>
        /// <returns>The <see cref="IList{T}"/> of resources corresponding to the specified logical path.</returns>
        IList<T> GetResources<T>(string resourcePath) where T : class;

        /// <summary>
        /// Gets a resource based on its logical path.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="resourcePath">The logical path of the resource.</param>
        /// <returns>The instance of the resource corresponding to the specified logical path.</returns>
        Task<T> GetResourceAsync<T>(string resourcePath) where T: class;

        /// <summary>
        /// Gets a resource based on its logical path.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="resourcePath">The logical path of the resource.</param>
        /// <returns>The instance of the resource corresponding to the specified logical path.</returns>
        T GetResource<T>(string resourcePath) where T : class;

        /// <summary>
        /// Executes an action based on its logical path.
        /// </summary>
        /// <param name="actionPath">The logical path of the action to be executed.</param>
        /// <returns>The <see cref="ResourceProviderActionResult"/> that contains details about the result of the execution.</returns>
        Task<ResourceProviderActionResult> ExecuteAction(string actionPath);

        /// <summary>
        /// Creates or updates a resource based on its logical path.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="resourcePath">The logical path of the resource.</param>
        /// <param name="resource">The instance of the resource being created or updated.</param>
        /// <returns></returns>
        Task UpsertResourceAsync<T>(string resourcePath, T resource) where T : class;

        /// <summary>
        /// Creates or updates a resource based on its logical path.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="resourcePath">The logical path of the resource.</param>
        /// <param name="resource">The instance of the resource being created or updated.</param>
        void UpsertResource<T>(string resourcePath, T resource) where T : class;

        /// <summary>
        /// Deletes a resource based on its logical path.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="resourcePath">The logical path of the resource.</param>
        /// <returns></returns>
        Task DeleteResourceAsync<T>(string resourcePath) where T : class;

        /// <summary>
        /// Deletes a resource based on its logical path.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="resourcePath">The logical path of the resource.</param>
        void DeleteResource<T>(string resourcePath) where T : class;
    }
}
