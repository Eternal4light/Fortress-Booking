using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PartnerService.Controllers;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using PartnerService.WebHost.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PartnerService.UnitTests
{
    public class ActivitiesControllerTests
    {
        private readonly Fixture _fixture = new();
        private readonly Mock<IRepository<Activity>> _repositoryMock = new();

        [Fact]
        public async Task GetActivitiesAsync_ReturnsAllActivities()
        {
            var activities = _fixture.CreateMany<Activity>(3).ToList();
            _repositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(activities);

            var sut = new ActivitiesController(_repositoryMock.Object);

            var result = await sut.GetActivitiesAsync();

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeAssignableTo<IEnumerable<ActivityResponse>>().Subject;
            response.Select(x => x.Id).Should().BeEquivalentTo(activities.Select(x => x.Id));
        }
    }
}
