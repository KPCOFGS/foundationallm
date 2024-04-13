﻿using FoundationaLLM.Vectorization.Models;

namespace FoundationaLLM.Vectorization.Interfaces
{
    /// <summary>
    /// Defines the interface for the Vectorization API client.
    /// </summary>
    public interface IVectorizationServiceClient
    {
        /// <summary>
        /// Processes an incoming vectorization request.
        /// </summary>
        /// <param name="vectorizationRequest">The <see cref="VectorizationRequest"/> object containing the details of the vectorization request.</param>
        /// <returns>The result of the request including the resource object id, success or failure plus any error messages.</returns>
        Task<VectorizationResult> ProcessRequest(VectorizationRequest vectorizationRequest);
    }
}
