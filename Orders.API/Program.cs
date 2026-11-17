using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString =  builder.Configuration["Blob:ConnectionString"];

var serviceUri = builder.Configuration["Blob:ServiceUri"];

builder.Services.AddSingleton<BlobServiceClient>(sp =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        return new BlobServiceClient(connectionString);
    }
    else if (!string.IsNullOrWhiteSpace(serviceUri))
    {
        return new BlobServiceClient(new Uri(serviceUri), new DefaultAzureCredential());
    }
    else
    {
        throw new InvalidOperationException("Blob storage configuration is missing.");
    }
});



var app = builder.Build();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        status = "Healthy",
        service = "Manufacturing Orders API"
    }));

app.MapPost("/orders", (CreateOrderRequest order) =>
{
    var orderId = Guid.NewGuid();   

    return Results.Created($"/orders/{orderId}", new
    {
        orderId = orderId,
        partNumber = order.PartNumber,
        quantity = order.Quantity,
        status = "Order received"
    }); 
    // Logic to process the order
});

app.MapPost("/documents/{orderId}",async(
    string orderId,IFormFile file, BlobServiceClient blobServiceClient,
    CancellationToken cancellationToken
    ) =>
{
    if (file == null || file.Length == 0)
        {
            return Results.BadRequest("Please upload a PDF.");
        }
    if (!Path.GetExtension(file.FileName)
            .Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Only PDF files are allowed.");
        }
         if (file.Length > 20 * 1024 * 1024)
        {
            return Results.BadRequest("Maximum file size is 20 MB.");
        }

    var containerName = builder.Configuration["Blob:ContainerName"]?? "manufacturing-documents";
    var containerClient =  blobServiceClient.GetBlobContainerClient(containerName);
    var blobName =
            $"{orderId}/{Guid.NewGuid():N}-{Path.GetFileName(file.FileName)}";
   var blobclient = containerClient.GetBlobClient(blobName); 
    await using var stream = file.OpenReadStream();

    await blobclient.UploadAsync(
        stream,
        new BlobUploadOptions { 
           HttpHeaders  = new BlobHttpHeaders 
           { ContentType = "application/pdf"}
        },
        cancellationToken

    );
    return Results.Ok(new
        {
            orderId,
            blobName,
            status = "Uploaded successfully"
        });
}).DisableAntiforgery();


app.Run();


record CreateOrderRequest(string PartNumber, int Quantity);    