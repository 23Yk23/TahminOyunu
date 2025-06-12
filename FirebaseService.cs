using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TahminOyunu.Services
{
    public class FirebaseService
    {
        private readonly IConfiguration _configuration;
        private readonly string _bucket;
        private readonly StorageClient _storageClient;

        public FirebaseService(IConfiguration configuration)
        {
            _configuration = configuration;
            _bucket = _configuration["Firebase:Bucket"];

            var credentialPath = _configuration["Firebase:CredentialPath"];
            GoogleCredential credential = GoogleCredential.FromFile(credentialPath);
            _storageClient = StorageClient.Create(credential);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            await _storageClient.UploadObjectAsync(_bucket, fileName, contentType, fileStream);
            return $"https://storage.googleapis.com/{_bucket}/{fileName}";
        }
    }
}
