using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using PartnerService.Controllers;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using PartnerService.WebHost.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PartnerService.UnitTests
{
    public class PartnersControllerTests
    {
        private readonly Fixture _fixture = new();
        private readonly Mock<IRepository<Partner>> _repositoryMock = new();
        private readonly Mock<IDistributedCache> _cacheMock = new();

        public PartnersControllerTests()
        {
            // по умолчанию кэш пуст — контроллер читает список из репозитория
            _cacheMock
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);
        }

        private PartnersController CreateSut() => new(_repositoryMock.Object, _cacheMock.Object);

        private Partner CreatePartner() =>
            _fixture.Build<Partner>()
                .Without(x => x.Invites)
                .Create();

        [Fact]
        public async Task GetPartnersAsync_ReturnsAllPartnersFromRepository()
        {
            var partners = Enumerable.Range(0, 3).Select(_ => CreatePartner()).ToList();

            _repositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(partners);

            var sut = CreateSut();

            var result = await sut.GetPartnersAsync();

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeAssignableTo<IEnumerable<PartnerResponse>>().Subject;
            response.Select(x => x.Id).Should().BeEquivalentTo(partners.Select(x => x.Id));
            response.Select(x => x.Capacity).Should().BeEquivalentTo(partners.Select(x => x.Capacity));
        }

        [Fact]
        public async Task GetPartnersAsync_ReturnsListFromCache_WithoutTouchingRepository()
        {
            var cached = new List<PartnerResponse>
            {
                new() { Id = System.Guid.NewGuid(), Name = "Из кэша", Capacity = 12, IsActive = true }
            };

            _cacheMock
                .Setup(x => x.GetAsync("partners:list", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cached)));

            var sut = CreateSut();

            var result = await sut.GetPartnersAsync();

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeAssignableTo<IEnumerable<PartnerResponse>>().Subject.ToList();
            response.Should().ContainSingle().Which.Capacity.Should().Be(12);
            _repositoryMock.Verify(x => x.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task CreatePartnerAsync_AddsPartnerAndReturnsIt()
        {
            var request = _fixture.Create<CreatePartnerRequest>();
            Partner? addedPartner = null;

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<Partner>()))
                .Callback<Partner>(p => addedPartner = p)
                .Returns(Task.CompletedTask);

            var sut = CreateSut();

            var result = await sut.CreatePartnerAsync(request);

            addedPartner.Should().NotBeNull();
            addedPartner!.Name.Should().Be(request.Name);
            addedPartner.Specialization.Should().Be(request.Specialization);
            addedPartner.IsActive.Should().BeTrue();
            addedPartner.Capacity.Should().Be(request.Capacity);

            var created = result.Result.Should().BeOfType<CreatedResult>().Subject;
            created.Location.Should().Be($"/partners/{addedPartner.Id}");
            var response = created.Value.Should().BeOfType<PartnerResponse>().Subject;
            response.Name.Should().Be(request.Name);
            response.Specialization.Should().Be(request.Specialization);
            response.IsActive.Should().BeTrue();
            response.Capacity.Should().Be(request.Capacity);

            _cacheMock.Verify(x => x.RemoveAsync("partners:list", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task CreatePartnerAsync_ReturnsBadRequest_WhenCapacityIsLessThanOne(int capacity)
        {
            var request = new CreatePartnerRequest { Name = "Коворкинг", Specialization = "Офисный", Capacity = capacity };

            var sut = CreateSut();

            var result = await sut.CreatePartnerAsync(request);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Partner>()), Times.Never);
        }
    }
}
