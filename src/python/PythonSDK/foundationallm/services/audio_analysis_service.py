import base64
import requests
from foundationallm.config import Configuration
from foundationallm.models.attachments import AttachmentProperties
from foundationallm.storage import BlobStorageManager

class AudioAnalysisService:
    """
    Performs audio analysis services.
    """

    def __init__(self, config: Configuration):
        self.config = config
        
    def _get_as_base64(self, storage_account_name, file_path: str) -> str:
        """
        Retrieves an image from its URL and converts it to a base64 string.

        Parameters
        ----------
        mime_type : str
            The mime type of the image.
        image_url : str
            The URL of the image.

        Returns
        -------
        str
            The image as a base64 string.
        """
        try:
            # Remove any leading slashes from the file path.
            file_path = file_path.lstrip('/')
            # Attempt to retrieve the image from blob storage.
            container_name = file_path.split('/')[0]
            # Get the file path without the container name.
            file_name = file_path.removeprefix(container_name)

            try:
                storage_manager = BlobStorageManager(
                    account_name=storage_account_name,
                    container_name=container_name,
                    authentication_type=self.config.get_value('FoundationaLLM:ResourceProviders:Attachment:Storage:AuthenticationType')
                )
            except Exception as e:
                raise Exception(f'Error connecting to the {storage_account_name} blob storage account and the container named {container_name}: {e}')

            if (storage_manager.file_exists(file_name)):
                try:
                    # Get the image file from blob storage.
                   audio_blob = storage_manager.read_file_content(file_name)
                   return base64.b64encode(audio_blob).decode('utf-8')
                except Exception as e:
                    raise Exception(f'The specified image {storage_account_name}/{file_path} does not exist.')
            else:
                raise Exception(f'The specified image {storage_account_name}/{file_path} does not exist.')
        except Exception as e:
            print(f'Error getting image as base64: {e}')
            return None

    def classify(self, audio_attachments: AttachmentProperties):
        api_key = self.config.get_value('FoundationaLLM:APIEndpoints:AudioClassificationAPI:APIKey')
        base_url = self.config.get_value('FoundationaLLM:APIEndpoints:AudioClassificationAPI:APIUrl').rstrip('/')
        deployment_name = self.config.get_value('FoundationaLLM:APIEndpoints:AudioClassificationAPI:DeploymentName')
        endpoint = self.config.get_value('FoundationaLLM:APIEndpoints:AudioClassificationAPI:PredictionEndpoint').lstrip('/')
        try:
            top_k = self.config.get_value('FoundationaLLM:APIEndpoints:AudioClassificationAPI:TopK')
        except:
            top_k = 1
        
        api_endpoint = f'{base_url}/{endpoint}'
        audio_analyses = {}

        for attachment in audio_attachments:
            if attachment.content_type.startswith('audio/'):
                # Check for WAV and MP3 files only.
                if not attachment.content_type.endswith('wav') and not attachment.content_type.endswith('mp3'):
                    # TODO: Handle this properly.
                    continue

                audio_base64 = self._get_as_base64(storage_account_name=attachment.provider_storage_account_name, file_path=attachment.provider_file_name)
                if audio_base64 and audio_base64 != '':
                    # Create the payload, sending the base64 encoded audio file.
                    payload = {
                        "file": audio_base64,
                        "content_type": attachment.content_type,
                        "deployment_name": deployment_name,
                        "top_k": top_k
                    }
                    headers = {"charset": "utf-8", "Content-Type": "application/json", "x-api-key": api_key}
                    # Make the REST API call.
                    r = requests.post(api_endpoint, json=payload, headers=headers)
                    # Check the response status code.
                    if r.status_code != 200:
                        raise Exception(f'Error: ({r.status_code}) {r.text}')
                    
                    response = r.json()

                    # Add the audio analysis to the dictionary.
                    audio_analyses[attachment.original_file_name] = response

        return audio_analyses
                    
