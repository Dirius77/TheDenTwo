using System.Linq;
using Content.Client.Humanoid;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._DEN.Markings;

[TestFixture]
[TestOf(typeof(MarkingsViewModel))]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class MarkingsViewModelTests : GameTest
{
    private MarkingsViewModel _model = default!;
    private MarkingManager _manager = default!;

    private static string[] _species = GameDataScrounger.PrototypesOfKind<SpeciesPrototype>();

    [SetUp]
    public async Task SetUp()
    {
        await Client.WaitPost(() =>
        {
            _model = new MarkingsViewModel();
            _manager = Client.ResolveDependency<MarkingManager>();
        });
    }

    [Test]
    [TestOf(typeof(SpeciesPrototype))]
    [TestCaseSource(nameof(_species))]
    [Description("Ensure that all roundstart species have at least 1 point per selectable marking layer.")]
    public async Task AllLayersHavePoints(string speciesId)
    {
        const int minimum = 1;
        var species = Client.ProtoMan.Index<SpeciesPrototype>(speciesId);

        // We don't care about species you can't edit in the character editor.
        if (!species.RoundStart)
            return;

        _model.OrganData = _manager.GetMarkingData(species);
        _model.OrganProfileData = _manager.GetProfileData(species, Sex.Male, Color.White, Color.White);
        _model.ValidateMarkings();

        // Iterate over all visual organs
        foreach (var (organ, organData) in _model.OrganData)
        {
            var layers = organData.Layers;
            var group = organData.Group;

            if (!_model.OrganProfileData.TryGetValue(organ, out var organProfileData))
                continue;

            // Iterate over all layers
            foreach (var layer in layers)
            {
                // Check that this layer has markings
                var layerMarkings = _model.EnforceGroupAndSexRestrictions
                    ? _manager.MarkingsByLayerAndGroupAndSex(layer, group, organProfileData.Sex)
                    : _manager.MarkingsByLayer(layer);

                if (layerMarkings.Count == 0)
                    continue;

                // Get the marking limits for this layer
                _model.GetMarkingCounts(organ, layer,
                    out var _,
                    out var limit,
                    out var _);

                // Make sure it's at least 1
                Assert.That(limit, Is.AtLeast(minimum).Or.EqualTo(-1),
                    $"Marking group {group.Id} has {limit} points for {layer}! Expected: {minimum}");
            }
        }
    }
}
