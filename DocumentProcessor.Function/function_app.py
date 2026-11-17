import azure.functions as func
import datetime
import json
import logging

from azure.identity import DefaultAzureCredential
from azure.storage.blob import BlobClient


app = func.FunctionApp()

@app.service_bus_queue_trigger(
    arg_name="msg",
    queue_name="document-processing",
    connection="SERVICE_BUS_CONNECTION"
)
def process_document(msg: func.ServiceBusMessage):
    try:
        message_body = msg.get_body().decode('utf-8')   

        event = json.loads(message_body)
        event_type = event.get("eventType")
        blob_url = event.get("data", {}).get("url")
        logging.info("Service Bus message received.")
        logging.info("Message content: %s", message_body)

        logging.info("Document processing event received successfully.")
        logging.info("Event Type: %s", event_type)
        logging.info("Blob URL: %s", blob_url)

        credential = DefaultAzureCredential()
        blob_client = BlobClient.from_blob_url(blob_url, credential=credential)
        blob_data = blob_client.download_blob().readall()

        logging.info("Blob downloaded successfully.")
        logging.info("Downloaded file size: %s bytes", len(blob_data))

    except Exception as e:
        logging.exception("Document processing failed")
        raise
