using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using PartnerService.WebHost.Services;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PartnerService.UnitTests
{
    public class UserServiceClientTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
            {
                _respond = respond;
            }

            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(_respond(request));
            }
        }

        private static UserServiceClient CreateSut(StubHandler handler)
        {
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(x => x["InternalApi:Key"]).Returns("internal-key");

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://user-service/") };
            return new UserServiceClient(httpClient, configuration.Object);
        }

        private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        [Fact]
        public async Task GetOccupiedSeatsAsync_ReturnsCountFromUserService()
        {
            var partnerId = Guid.NewGuid();
            var handler = new StubHandler(_ => Json("{\"occupiedSeats\":7}"));

            var occupied = await CreateSut(handler).GetOccupiedSeatsAsync(partnerId, CancellationToken.None);

            occupied.Should().Be(7);
            handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
            handler.LastRequest.RequestUri!.ToString()
                .Should().Be($"http://user-service/api/users/invites/occupancy?partnerId={partnerId}");
            handler.LastRequest.Headers.GetValues("X-Internal-Api-Key").Single().Should().Be("internal-key");
        }

        [Fact]
        public async Task GetOccupiedSeatsAsync_ReturnsNull_WhenUserServiceAnswersWithError()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

            var occupied = await CreateSut(handler).GetOccupiedSeatsAsync(Guid.NewGuid(), CancellationToken.None);

            occupied.Should().BeNull();
        }

        [Fact]
        public async Task GetOccupiedSeatsAsync_ReturnsNull_WhenUserServiceIsUnavailable()
        {
            var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

            var occupied = await CreateSut(handler).GetOccupiedSeatsAsync(Guid.NewGuid(), CancellationToken.None);

            occupied.Should().BeNull();
        }

        [Fact]
        public async Task GetOccupiedSeatsAsync_ReturnsNull_WhenResponseIsNotJson()
        {
            var handler = new StubHandler(_ => Json("не json"));

            var occupied = await CreateSut(handler).GetOccupiedSeatsAsync(Guid.NewGuid(), CancellationToken.None);

            occupied.Should().BeNull();
        }
    }
}
