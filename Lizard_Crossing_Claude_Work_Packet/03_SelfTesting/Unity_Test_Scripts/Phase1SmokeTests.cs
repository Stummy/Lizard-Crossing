using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Phase1SmokeTests
{
    [Test]
    public void Phase1Scene_HasCoreObjects()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        Assert.IsNotNull(player, "Missing Player object with tag 'Player'.");

        var camera = Camera.main;
        Assert.IsNotNull(camera, "Missing Main Camera tagged as MainCamera.");

        Assert.IsTrue(Object.FindObjectsOfType<MonoBehaviour>().Any(m => m.GetType().Name.Contains("GameState")),
            "Missing GameStateManager-like script.");

        Assert.IsTrue(Object.FindObjectsOfType<Collider>().Any(c => c.gameObject.name.ToLower().Contains("safe")),
            "Missing Safe Zone collider/object. Name should include 'Safe'.");
    }

    [Test]
    public void Phase1Scene_HasHazardsAndCollectibles()
    {
        var allObjects = Object.FindObjectsOfType<GameObject>();

        Assert.IsTrue(allObjects.Any(o => o.name.ToLower().Contains("hazard") || o.name.ToLower().Contains("shoe") || o.name.ToLower().Contains("foot")),
            "Missing hazard objects. Add sideways-moving shoes/feet.");

        Assert.IsTrue(allObjects.Any(o => o.name.ToLower().Contains("bug") || o.name.ToLower().Contains("collectible")),
            "Missing bug/collectible objects.");
    }

    [Test]
    public void MainCamera_IsLowEnoughForLizardPOV()
    {
        var camera = Camera.main;
        Assert.IsNotNull(camera, "Missing Main Camera.");

        Assert.Less(camera.transform.position.y, 3.0f,
            "Camera is probably too high for low lizard POV. Adjust threshold based on final scale.");
    }
}
