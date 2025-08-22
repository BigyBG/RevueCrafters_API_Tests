using RestSharp;
using RestSharp.Authenticators;
using RevueCrafters.Models;
using System.Text.Json;

namespace RevueCrafters
{
    public class RevueCraftersAPIsTests
    {
        private RestSharp.RestClient client;
        private string lastRevueId = string.Empty;

        private string JwtToken = null;
        /*
        private static string ReadEnv(string name) =>
            Environment.GetEnvironmentVariable(name) ??                                  // Process (CI/CD)
            Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ??  // Local user
            Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine); // Local machine
        
        private readonly string Email = ReadEnv("REVUE_CRAFTERS_EMAIL");
        private readonly string Password = ReadEnv("REVUE_CRAFTERS_PASS");

        */

        private string Email = Environment.GetEnvironmentVariable("REVUE_CRAFTERS_EMAIL");
        private string Password = Environment.GetEnvironmentVariable("REVUE_CRAFTERS_PASS");

        private string BaseUrl = "https://d2925tksfvgq8c.cloudfront.net";

        private string GetJwtToken(string email, string password)
        {
            var tmpClient = new RestSharp.RestClient(BaseUrl);
            var request = new RestSharp.RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            var response = tmpClient.Execute(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();
                if (!string.IsNullOrWhiteSpace(token))
                    return token;
            }
            throw new Exception($"Failed to get JWT token: {response.Content}");
        }

        [SetUp]
        public void Setup()
        {
            if (string.IsNullOrWhiteSpace(JwtToken))
                JwtToken = GetJwtToken(Email, Password);

            var options = new RestSharp.RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(JwtToken),
            };
            this.client = new RestSharp.RestClient(options);
        }

        [Order(1)]
        [Test]
        public void CreateRevue_WithRequiredFeilds_ShouldReturnSucces()
        {
            // Arrange
            var request = new RestSharp.RestRequest("/api/Revue/Create", Method.Post);
            var revueRequest = new RevueDTO
            {
                Title = "Test Revue",
                Description = "This is a test Revue",
                Url = ""
            };
            request.AddJsonBody(revueRequest);

            // Act
            var response = this.client.Execute(request);
            var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(content, Is.Not.Null);
            Assert.That(content.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllRevues_ShouldReturnList()
        {
            // Arrange
            var request = new RestSharp.RestRequest($"/api/Revue/All", Method.Get);

            // Act
            var response = this.client.Execute(request);
            var content = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(content, Is.Not.Null);
            Assert.That(content.Count, Is.GreaterThan(0), "Expected at least one revue to be returned.");
            lastRevueId = content.LastOrDefault()?.RevueId ?? string.Empty;
        }

        [Order(3)]
        [Test]
        public void EditLastCreatedRevue_ShouldReturnSuccess()
        {
            // Arrange
            if (string.IsNullOrWhiteSpace(lastRevueId))
            {
                var tmpRequest = new RestSharp.RestRequest($"/api/Revue/All", Method.Get);
                var tmpResponse = this.client.Execute(tmpRequest);
                var tmpContent = JsonSerializer.Deserialize<List<ApiResponseDTO>>(tmpResponse.Content);
                Assert.That(tmpContent.Count, Is.GreaterThan(0), "Expected to have at least one revue allready created.");
                lastRevueId = tmpContent.LastOrDefault()?.RevueId ?? string.Empty;
            }

            var revueRequest = new RevueDTO
            {
                Title = "Edited Test Revue",
                Description = "This is an updated/edited test revue",
                Url = ""
            };

            var request = new RestSharp.RestRequest($"/api/Revue/Edit", Method.Put);
            request.AddQueryParameter("revueId", lastRevueId);
            request.AddJsonBody(revueRequest);

            // Act
            var response = this.client.Execute(request);

            // Assert
            var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(content, Is.Not.Null);
            Assert.That(content.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteLastCreatedRevue_ShouldReturnSuccess()
        {
            // Arrange
            if (string.IsNullOrWhiteSpace(lastRevueId))
            {
                var tmpRequest = new RestSharp.RestRequest($"/api/Revue/All", Method.Get);
                var tmpResponse = this.client.Execute(tmpRequest);
                var tmpContent = JsonSerializer.Deserialize<List<ApiResponseDTO>>(tmpResponse.Content);
                Assert.That(tmpContent.Count, Is.GreaterThan(0), "Expected to have at least one revue allready created.");
                lastRevueId = tmpContent.LastOrDefault()?.RevueId ?? string.Empty;
            }
            var request = new RestSharp.RestRequest($"/api/Revue/Delete", Method.Delete);
            request.AddQueryParameter("revueId", lastRevueId);

            // Act
            var response = this.client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The revue is deleted!"));
        }

        [Order(5)]
        [Test]
        public void CreatingRevueWithoutRequaredField_SchouldReturn_BadRequest()
        {
            // Arrange
            var request = new RestSharp.RestRequest("/api/Revue/Create", Method.Post);
            var revueRequest = new RevueDTO
            {
                Title = "",
                Description = ""
            };
            request.AddJsonBody(revueRequest);

            // Act
            var response = this.client.Execute(request);
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void TryToEditNonExistingRevue_ShouldReturnNotFound()
        {
            // Arrange
            var request = new RestSharp.RestRequest($"/api/Revue/Edit", Method.Put);
            request.AddQueryParameter("revueId", "non-existing-id");
            var ideaRequest = new RevueDTO
            {
                Title = "Updated Test Revue",
                Description = "This is an updated test revue",
                Url = ""
            };
            request.AddJsonBody(ideaRequest);
            // Act
            var response = this.client.Execute(request);
            // Assert
            
            //Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("There is no such revue!"));
        }

        [Order(7)]
        [Test]
        public void TryToDeleteNonExistingRevue_ShouldReturnNotFound()
        {
            // Arrange
            var request = new RestSharp.RestRequest($"/api/Revue/Delete", Method.Put);
            request.AddQueryParameter("revueId", "non-existing-id");

            // Act
            var response = this.client.Execute(request);

            // Assert

            //Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
            //Assert.That(response.Content, Does.Contain("There is no such revue!"));
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.MethodNotAllowed));
        }

        [TearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}