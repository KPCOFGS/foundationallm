"""
Main entry-point for the FoundationaLLM LangChainAPI.
Runs web server exposing the API.
"""
from fastapi import FastAPI
from app.dependencies import get_config
from app.routers import (
    manage,
    orchestration,
    status
)
from foundationallm.telemetry import Telemetry

# Open a connection to the app configuration
config = get_config()
# Start collecting telemetry
Telemetry.configure_monitoring(config, 'FoundationaLLM:APIs:LangChainAPI:AppInsightsConnectionString')

app = FastAPI(
    title='FoundationaLLM LangChainAPI',
    summary='API for interacting with large language models using the LangChain orchestrator.',
    description="""The FoundationaLLM LangChainAPI is a wrapper around LangChain functionality
                contained in the foundationallm.core Python SDK.""",
    version='1.0.0',
    contact={
        'name':'Solliance, Inc.',
        'email':'contact@solliance.net',
        'url':'https://solliance.net/' 
    },
    openapi_url='/swagger/v1/swagger.json',
    docs_url='/swagger',
    redoc_url=None,
    license_info={
        'name': 'FoundationaLLM Software License',
        'url': 'https://www.foundationallm.ai/license',
    },
    config=config
)

app.include_router(manage.router)
app.include_router(orchestration.router)
app.include_router(status.router)

@app.get('/')
async def root():
    """
    Root path of the API.
    
    Returns
    -------
    str
        Returns a JSON object containing a message and value.
    """
    return { 'message': 'FoundationaLLM LangChainAPI' }
