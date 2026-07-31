using NUnit.Framework;
using UnityEngine;

public sealed class ObjectiveProgressTests
{
    [Test]
    public void Progress_ClampsAtTargetAndCompletes()
    {
        ObjectiveDefinition definition =
            ScriptableObject.CreateInstance<
                ObjectiveDefinition>();

        definition.TargetAmount = 3;

        ObjectiveProgress progress =
            new ObjectiveProgress(definition);

        Assert.That(progress.AddProgress(5),Is.True);
        Assert.That(progress.Progress,Is.EqualTo(3));
        Assert.That(progress.Completed,Is.True);
        Assert.That(progress.AddProgress(1),Is.False);

        Object.DestroyImmediate(definition);
    }
}
