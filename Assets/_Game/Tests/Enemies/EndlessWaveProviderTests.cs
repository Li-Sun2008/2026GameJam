#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Spotlight.Contracts;
using Spotlight.Enemies;
namespace Spotlight.Tests.Enemies {
public sealed class EndlessWaveProviderTests {
 [Test] public void Night51ResolvesTemplateAndGrowthOnceWithoutMutatingTemplate(){
  var group=new SpawnGroupDefinition{Id="g",EnemyId="enemy",Count=3,HpMultiplier=2,DamageMultiplier=3,ApplyDayGrowth=true};
  var template=new WaveDefinition{Id="template",Groups=new[]{group}};
  var endless=new EndlessDefinition{WaveTemplateIds=new[]{"a","b"},HpGrowthPerNight=.1f,DamageGrowthPerNight=.2f};
  string selected=null;var provider=new EndlessWaveProvider();var first=provider.Resolve(51,endless,delegate(string id){selected=id;return template;});
  Assert.AreEqual("a",selected);Assert.AreEqual(12,first[0].Groups[0].HpMultiplier,.0001f);Assert.AreEqual(33,first[0].Groups[0].DamageMultiplier,.0001f);Assert.IsFalse(first[0].Groups[0].ApplyDayGrowth);
  Assert.AreEqual(2,group.HpMultiplier);Assert.IsTrue(group.ApplyDayGrowth);first[0].Groups[0].Count=99;
  Assert.AreEqual(3,provider.Resolve(51,endless,delegate(string id){return template;})[0].Groups[0].Count);
 }
 [Test] public void MissingTemplatesDoNotInventWave(){Assert.AreEqual(0,new EndlessWaveProvider().Resolve(1,new EndlessDefinition{WaveTemplateIds=new string[0]},delegate(string id){return null;}).Length);}
}
}
#endif
