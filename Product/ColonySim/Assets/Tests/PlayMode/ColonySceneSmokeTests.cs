using System.Collections;
using ColonySim.Domain;
using ColonySim.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColonySim.Tests.PlayMode
{
    public sealed class ColonySceneSmokeTests
    {
        [UnityTest]
        public IEnumerator SampleScene_BuildsVisibleScenarioAndAdvancesOneStep()
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;

            var controller = Object.FindAnyObjectByType<ColonyScenarioController>();
            Assert.That(controller, Is.Not.Null);

            controller.RebuildScenario(HealthModelSelection.Functional, false);
            yield return null;

            Assert.That(controller.PawnViewCount, Is.EqualTo(3));
            Assert.That(controller.TaskViewCount, Is.EqualTo(3));
            Assert.That(controller.Simulation.StepNumber, Is.EqualTo(0));

            controller.StepOnce();

            Assert.That(controller.Simulation.StepNumber, Is.EqualTo(1));
            Assert.That(controller.Simulation.Events, Is.Not.Empty);
        }
    }
}
