#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Spotlight.Bootstrap;
using Spotlight.Contracts;
using Spotlight.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Spotlight.Tests.Integration
{
    public sealed class HitFlashIntegrationTests
    {
        private GameObject host;
        private ModuleComposition runtime;
        private ViewPool pool;
        private string directory;
        private CommandContext Command() { return runtime.Core.Commands.Create(runtime.Core.Transactions.Revision); }
        private static void Draw(RuntimeWorldRenderer2D renderer)
        { typeof(RuntimeWorldRenderer2D).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, null); }
        private static GameObject View(RuntimeWorldRenderer2D renderer, EntityId id)
        {
            IDictionary entries = (IDictionary)typeof(RuntimeWorldRenderer2D).GetField("views", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(renderer);
            object entry = entries["actor:" + id.Value];
            Assert.NotNull(entry);
            return (GameObject)entry.GetType().GetField("View", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entry);
        }
        [Test]
        public void ZeroTimeLateBodyBindingInvalidInputsAndRepeatedHitsRestoreExactColor()
        {
            host = new GameObject("Hit flash late binding");
            HitFlashVisual2D flash = host.AddComponent<HitFlashVisual2D>();
            Assert.IsNull(host.GetComponent<SpriteRenderer>(), "Root must not gain an empty sprite renderer");
            GameObject body = new GameObject("Body"); body.transform.SetParent(host.transform);
            SpriteRenderer sprite = body.AddComponent<SpriteRenderer>();
            Color original = new Color(.2f, .4f, .6f, .7f); sprite.color = original;
            flash.BodyRenderer = sprite;
            flash.PlayHit(0); Assert.AreNotEqual(original, sprite.color);
            flash.ApplyVisualTime(double.NaN); Assert.AreNotEqual(original, sprite.color);
            flash.PlayHit(.1); flash.ApplyVisualTime(.15); Assert.AreNotEqual(original, sprite.color);
            flash.ApplyVisualTime(.23); Assert.AreEqual(original, sprite.color);
            flash.PlayHit(double.PositiveInfinity); Assert.AreEqual(original, sprite.color);
            flash.DurationSeconds = float.NaN; flash.PlayHit(0); Assert.AreEqual(original, sprite.color);
            sprite.color = Color.white; flash.BodyRenderer = null; flash.ResetVisual();
            flash.BodyRenderer = sprite; flash.DurationSeconds = .12f;
            flash.PlayHit(0); Assert.AreEqual(flash.FlashColor, sprite.color, "Reusable component must apply the configured color exactly");
            flash.ResetVisual(); Assert.AreEqual(Color.white, sprite.color);
        }

        [UnityTest]
        public IEnumerator RealDamageBeforeFirstRentFreezesWithPauseAndExpiresAtDoubleSpeed()
        {
            yield return new EnterPlayMode();
            PrototypeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(PrototypeCatalogFactory.CatalogPath);
            Assert.NotNull(catalog);
            directory = Path.Combine(Path.GetTempPath(), "Spotlight_HitFlash_" + Guid.NewGuid().ToString("N"));
            runtime = new ModuleComposition(catalog.BuildCatalogData(), directory);
            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
            foreach (PrefabEntry entry in catalog.PrefabEntries) prefabs.Add(entry.Key, entry.Prefab);
            pool = new ViewPool(prefabs);
            host = new GameObject("Hit flash real renderer");
            RuntimeWorldRenderer2D renderer = host.AddComponent<RuntimeWorldRenderer2D>(); renderer.enabled = false;
            renderer.Bind(runtime, pool);
            Assert.AreEqual(OperationState.Committed, runtime.Flow.TryNewRun(Command(), new NewRunRequest { LevelId = "level.prototype", Mode = GameMode.Story, Seed = 123 }).State);
            runtime.Advance(0);
            DayEventSnapshot day = runtime.Gameplay.DayEvents.GetSnapshot();
            Assert.AreEqual(OperationState.Committed, runtime.Gameplay.DayEvents.TryChoose(Command(), day.State.InstanceId, day.Options[0].Id).State);
            runtime.Advance(0);
            if (!runtime.Flow.GetSnapshot().TutorialShown) runtime.Flow.TryDismissTutorial(Command());
            Assert.AreEqual(OperationState.Committed, runtime.Flow.TryStartNight(Command()).State);
            runtime.Advance(1f / 30);
            ActorSnapshot enemy = runtime.Core.World.GetActors(ActorKind.Enemy)[0];
            runtime.Combat.Damage.Enqueue(new DamagePacket { PacketId = "hit.flash.real", Source = runtime.Core.World.SpringId, Target = enemy.Id, Origin = DamageOrigin.Event, PhysicalDamage = 1 });
            runtime.Advance(1f / 30); // No view exists when the formal damage event is dispatched.
            ActorSnapshot damaged; Assert.IsTrue(runtime.Core.World.TryGetActor(enemy.Id, out damaged));
            Assert.Less(damaged.CurrentHp, enemy.CurrentHp);
            Draw(renderer);
            GameObject view = View(renderer, enemy.Id);
            HitFlashVisual2D flash = view.GetComponent<HitFlashVisual2D>(); Assert.NotNull(flash);
            Assert.AreSame(view.GetComponent<EnemyFacingVisual2D>().BodyRenderer, flash.BodyRenderer);
            Color hit = flash.BodyRenderer.color;
            EnemyHealthBar2D health = view.GetComponent<EnemyHealthBar2D>();
            SpriteRenderer bar = health.BarRoot.GetComponentInChildren<SpriteRenderer>(); Color barColor = bar.color;
            Guid pause = runtime.Clock.AcquirePause(PauseReason.Player);
            runtime.Advance(1); Draw(renderer); Assert.AreEqual(hit, flash.BodyRenderer.color); Assert.AreEqual(barColor, bar.color);
            runtime.Clock.ReleasePause(pause);
            Assert.AreEqual(OperationState.Committed, runtime.Clock.TrySetSpeed(Command(), GameSpeed.Double).State);
            runtime.Advance(.1f); Draw(renderer); Assert.AreNotEqual(hit, flash.BodyRenderer.color);
            Color restored = flash.BodyRenderer.color;
            runtime.Events.Publish(new DamageAppliedEvent { Result = new DamageResult { Target = enemy.Id, FinalDamage = 0 } }); runtime.Events.Flush();
            Draw(renderer); Assert.AreEqual(restored, flash.BodyRenderer.color, "Zero damage must not flash");
            flash.PlayHit(runtime.Clock.GetSnapshot().GameTime);
            renderer.Unbind(); Assert.AreEqual(restored, flash.BodyRenderer.color); Assert.IsFalse(view.activeSelf);
            renderer.Bind(runtime, pool); Draw(renderer);
            Assert.AreSame(view, View(renderer, enemy.Id)); Assert.AreEqual(restored, flash.BodyRenderer.color, "Pool reuse must not inherit hit state");
            Assert.AreEqual(OperationState.Committed, runtime.Flow.TryReturnToMenu(Command()).State); Draw(renderer);
            Assert.IsFalse(view.activeSelf);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (host != null)
            {
                RuntimeWorldRenderer2D renderer = host.GetComponent<RuntimeWorldRenderer2D>();
                if (renderer != null) renderer.Unbind();
            }
            if (runtime != null) runtime.Dispose();
            if (pool != null) pool.Clear();
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            host = null; runtime = null; pool = null;
            if (directory != null && Directory.Exists(directory))
            {
                string absolute = Path.GetFullPath(directory);
                Assert.IsTrue(absolute.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) && Path.GetFileName(absolute).StartsWith("Spotlight_HitFlash_", StringComparison.Ordinal));
                Directory.Delete(absolute, true);
            }
            directory = null;
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
#endif
