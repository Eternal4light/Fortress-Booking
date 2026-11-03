using AutoFixture;
using FluentAssertions;
using PartnerService.Core.Domain;
using PartnerService.DataAccess.Repositories;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PartnerService.UnitTests
{
    public class InMemoryRepositoryTests
    {
        private readonly Fixture _fixture = new();

        private Partner CreatePartner() =>
            _fixture.Build<Partner>()
                .Without(x => x.Invites)
                .Create();

        [Fact]
        public async Task GetAllAsync_ReturnsSeededEntities()
        {
            var partner = CreatePartner();
            var repository = new InMemoryRepository<Partner>(new[] { partner });

            var result = await repository.GetAllAsync();

            result.Should().ContainSingle(x => x.Id == partner.Id);
        }

        [Fact]
        public async Task AddAsync_AddsNewEntity_SoItAppearsInGetAllAsync()
        {
            var repository = new InMemoryRepository<Partner>(Enumerable.Empty<Partner>());
            var partner = CreatePartner();

            await repository.AddAsync(partner);
            var result = await repository.GetAllAsync();

            result.Should().ContainSingle(x => x.Id == partner.Id && x.Name == partner.Name);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMatchingEntity()
        {
            var partner = CreatePartner();
            var repository = new InMemoryRepository<Partner>(new[] { partner });

            var result = await repository.GetByIdAsync(partner.Id);

            result.Should().NotBeNull();
            result.Id.Should().Be(partner.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenEntityDoesNotExist()
        {
            var repository = new InMemoryRepository<Partner>(Enumerable.Empty<Partner>());

            var result = await repository.GetByIdAsync(System.Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_ReplacesExistingEntity()
        {
            var partner = CreatePartner();
            var repository = new InMemoryRepository<Partner>(new[] { partner });

            partner.Name = "Обновлённое имя";
            await repository.UpdateAsync(partner);

            var result = await repository.GetByIdAsync(partner.Id);
            result.Name.Should().Be("Обновлённое имя");
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntity()
        {
            var partner = CreatePartner();
            var repository = new InMemoryRepository<Partner>(new[] { partner });

            await repository.DeleteAsync(partner);
            var result = await repository.GetAllAsync();

            result.Should().BeEmpty();
        }
    }
}
