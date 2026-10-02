#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Spotlight.Contracts;
using Spotlight.Core.Data;
using Spotlight.Elements.Definitions;
using Spotlight.Combat.Definitions;
using Spotlight.Enemies.Definitions;
using Spotlight.Gameplay.Definitions;
using ElementAmountConfig = Spotlight.Elements.Definitions.ElementAmountsConfig;
using CombatAmountConfig = Spotlight.Combat.Definitions.ElementAmountsConfig;
using EnemyResourceConfig = Spotlight.Enemies.Definitions.ResourceAmountConfig;
using CombatResourceConfig = Spotlight.Combat.Definitions.ResourceAmountConfig;
using GameEffectConfig = Spotlight.Gameplay.Definitions.EffectConfig;
using ElementEffectConfig = Spotlight.Elements.Definitions.EffectConfig;
using EnemyEffectConfig = Spotlight.Enemies.Definitions.EffectConfig;
namespace Spotlight.Bootstrap
{
    // 原型数值全部落到可编辑资产；已有资产始终保留。
    public static class PrototypeCatalogFactory
    {
        public const string CatalogPath="Assets/_Game/Data/Prototype/P1/PrototypeCatalog.asset";
        const string Root="Assets/_Game/Data/Prototype/";
        static T Asset<T>(string folder,string name,Action<T> initialize) where T:ScriptableObject
        {
            string path=Root+folder+"/"+name+".asset";T asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset!=null)return asset;if(File.Exists(path))throw new InvalidOperationException("保留已有资产，无法按指定类型读取："+path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));asset=ScriptableObject.CreateInstance<T>();initialize(asset);AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static ElementAmountConfig Amount(ElementType type,float value)
        {
            ElementAmountConfig result=new ElementAmountConfig();switch(type){case ElementType.Water:result.Water=value;break;case ElementType.Fire:result.Fire=value;break;case ElementType.Earth:result.Earth=value;break;case ElementType.Wood:result.Wood=value;break;case ElementType.Wind:result.Wind=value;break;case ElementType.Thunder:result.Thunder=value;break;}return result;
        }
        static SpawnGroupConfig Group(string id,string enemy,int count,bool growth)
        {
            return new SpawnGroupConfig {Id=id,EnemyId=enemy,Count=count,SpawnPointId="spawn.ground",PathId="path.ground",SpawnIntervalSeconds=1,ApplyDayGrowth=growth,HpMultiplier=1,DamageMultiplier=1};
        }
        public static PrototypeCatalogSO CreateAssets(){return CreateAssets(new PrefabEntry[0]);}
        public static PrototypeCatalogSO CreateAssets(PrefabEntry[] prefabs)
        {
            PrototypeCatalogSO existing=AssetDatabase.LoadAssetAtPath<PrototypeCatalogSO>(CatalogPath);if(existing!=null)return existing;if(File.Exists(CatalogPath))throw new InvalidOperationException("保留已有Catalog，无法读取："+CatalogPath);
            GameConfigSO settings=Asset<GameConfigSO>("P1","GameConfig",delegate(GameConfigSO a){a.ConfigVersion="prototype.1";a.StoryDays=7;a.LogicTicksPerGameSecond=30;a.SpringMaxHp=100;a.SpringCollisionRadiusInCells=.35f;a.MaxReactionActionsPerTick=256;a.SpecialSkillId="skill.spotlight";});
            ResourceDefinitionSO gold=Asset<ResourceDefinitionSO>("P1","Gold",delegate(ResourceDefinitionSO a){a.Id="res.gold";a.DisplayNameKey="金币";a.IconKey="ResourceGold";});
            List<ElementDefinitionSO> elements=new List<ElementDefinitionSO>();List<ProductionRuleSO> production=new List<ProductionRuleSO>();List<StatusEffectDefinitionSO> statuses=new List<StatusEffectDefinitionSO>();List<GaugeConfigSO> gauges=new List<GaugeConfigSO>();
            string[] ids={"water","fire","earth","wood","wind","thunder"};string[] names={"水","火","土","木","风","雷"};
            for(int i=0;i<6;i++)
            {
                string id=ids[i],display=names[i];ElementType type=(ElementType)(i+1);
                elements.Add(Asset<ElementDefinitionSO>("P2","Element_"+id,delegate(ElementDefinitionSO a){a.Id="elm."+id;a.Element=type;a.DisplayNameKey=display;a.PrefabKey="Element"+type;a.BlocksGround=type==ElementType.Earth||type==ElementType.Wood;a.BaseStatusId="status."+id;a.ProjectileModifier=new ProjectileModifierConfig {Element=type,DamageAdd=Amount(type,2),GaugeAdd=Amount(type,25)};a.ContactEffects=new ElementEffectConfig[0];a.ContactIntervalSeconds=1;}));
                production.Add(Asset<ProductionRuleSO>("P2","Production_"+id,delegate(ProductionRuleSO a){a.Id="production."+id;a.Enabled=true;a.Match=ProductionMatch.SingleBlock;a.A=type;a.B=ElementType.None;a.ConsumeA=false;a.ConsumeB=false;a.OutputElementId="elm."+id;a.OutputCount=1;}));
                statuses.Add(Asset<StatusEffectDefinitionSO>("P3","Status_"+id,delegate(StatusEffectDefinitionSO a){a.Id="status."+id;a.ElementTag=type;a.IsReactionInput=true;a.IsReactionDamage=false;a.DurationSeconds=6;a.TickIntervalSeconds=1;a.MaxStacks=10;}));
                gauges.Add(Asset<GaugeConfigSO>("P3","Gauge_"+id,delegate(GaugeConfigSO a){a.Element=type;a.MaxValue=100;a.Threshold=100;a.DecayPerSecond=5;a.DecayDelaySeconds=1;a.IncomingMultiplier=1;}));
            }
            statuses.Add(Asset<StatusEffectDefinitionSO>("P3","Status_Entangle",delegate(StatusEffectDefinitionSO a){a.Id="status.entangle";a.ElementTag=ElementType.None;a.IsReactionInput=false;a.IsReactionDamage=true;a.DurationSeconds=3;a.TickIntervalSeconds=1;a.MaxStacks=5;a.MoveSlowPerStack=.1f;}));
            statuses.Add(Asset<StatusEffectDefinitionSO>("P3","Status_ThunderBurn",delegate(StatusEffectDefinitionSO a){a.Id="status.thunderburn";a.ElementTag=ElementType.None;a.IsReactionInput=false;a.IsReactionDamage=true;a.DurationSeconds=3;a.TickIntervalSeconds=1;a.MaxStacks=5;a.DotElementPerStack=new CombatAmountConfig {Thunder=2};}));
            statuses.Add(Asset<StatusEffectDefinitionSO>("P3","Status_ThunderStorm",delegate(StatusEffectDefinitionSO a){a.Id="status.thunderstorm";a.ElementTag=ElementType.None;a.IsReactionInput=false;a.IsReactionDamage=true;a.DurationSeconds=3;a.TickIntervalSeconds=1;a.MaxStacks=5;a.DotElementPerStack=new CombatAmountConfig {Thunder=2};a.AttackSlowPerStack=.05f;}));
            List<ReactionDefinitionSO> reactions=new List<ReactionDefinitionSO>();
            // 21个无序组合分别留在状态域与子弹域；仅文档授权的三种状态反应启用。
            for(int x=1;x<=6;x++)for(int y=x;y<=6;y++)for(int context=0;context<2;context++)
            {
                ElementType aa=(ElementType)x,bb=(ElementType)y;ReactionContext rc=(ReactionContext)context;string status=null;if(rc==ReactionContext.EnemyStatus){if(aa==ElementType.Wood&&bb==ElementType.Wind)status="status.entangle";if(aa==ElementType.Wood&&bb==ElementType.Thunder)status="status.thunderburn";if(aa==ElementType.Wind&&bb==ElementType.Thunder)status="status.thunderstorm";}string output=status;
                reactions.Add(Asset<ReactionDefinitionSO>("P2","Reaction_"+rc+"_"+aa+"_"+bb,delegate(ReactionDefinitionSO a){a.Id="reaction."+rc+"."+aa+"."+bb;a.Context=rc;a.A=aa;a.B=bb;a.ConsumeA=1;a.ConsumeB=1;a.Priority=100;a.Enabled=output!=null;a.Effects=output==null?new ElementEffectConfig[0]:new ElementEffectConfig[] {new ElementEffectConfig {Id="apply",Kind=EffectKind.ApplyStatus,TargetPolicy=EffectTargetPolicy.SelectedActor,ReferenceId=output,Stacks=1}};}));
            }
            TowerDefinitionSO tower=Asset<TowerDefinitionSO>("P3","TowerBasic",delegate(TowerDefinitionSO a){a.Id="tower.basic";a.PrefabKey="TowerBasic";a.HpPerStack=20;a.InitialAttackStacks=1;a.MaxAttackStacks=10;a.BasePhysicalDamage=5;a.DamagePerExtraStack=5;a.AttackIntervalSeconds=1.2f;a.RangeInCells=3;a.CollisionRadiusInCells=.35f;a.ProjectileId="projectile.basic";a.BuildCost=new CombatResourceConfig[] {new CombatResourceConfig {Bucket=ResourceBucket.Inventory,DefinitionId="res.gold",Amount=20}};a.PlacementLimit=0;a.RecallRefundRatio=.5f;});
            ProjectileDefinitionSO projectile=Asset<ProjectileDefinitionSO>("P3","ProjectileBasic",delegate(ProjectileDefinitionSO a){a.Id="projectile.basic";a.PrefabKey="ProjectileBasic";a.SpeedInCellsPerSecond=6;a.LifetimeSeconds=10;a.CollisionRadiusInCells=.1f;});
            ProjectileDefinitionSO enemyProjectile=Asset<ProjectileDefinitionSO>("P3","ProjectileEnemy",delegate(ProjectileDefinitionSO a){a.Id="projectile.enemy";a.PrefabKey="EnemyProjectile";a.SpeedInCellsPerSecond=6;a.LifetimeSeconds=10;a.CollisionRadiusInCells=.1f;});
            SpecialSkillDefinitionSO skill=Asset<SpecialSkillDefinitionSO>("P3","SpotlightSkill",delegate(SpecialSkillDefinitionSO a){a.Id="skill.spotlight";a.UsesPerNight=1;a.DurationSeconds=10;a.ReactionDamageMultiplier=1.2f;});
            DropTableSO drop=Asset<DropTableSO>("P4","GoldDrop",delegate(DropTableSO a){a.Id="drop.gold";a.Rolls=1;a.Entries=new DropEntryConfig[] {new DropEntryConfig {Id="gold",Weight=1,Bucket=ResourceBucket.Inventory,ResourceId="res.gold",MinAmount=5,MaxAmountInclusive=10}};});
            EnemyDefinitionSO melee=Asset<EnemyDefinitionSO>("P4","GoblinMelee",delegate(EnemyDefinitionSO a){a.Id="enemy.goblin.melee";a.PrefabKey="EnemyGoblinMelee";a.MaxHp=30;a.PhysicalDamage=5;a.SpeedInCellsPerSecond=1;a.RangeInCells=1;a.AttackIntervalSeconds=1;a.Enabled=true;a.CollisionRadiusInCells=.35f;a.DropTableId="drop.gold";});
            EnemyDefinitionSO ranged=Asset<EnemyDefinitionSO>("P4","GoblinRanged",delegate(EnemyDefinitionSO a){a.Id="enemy.goblin.ranged";a.PrefabKey="EnemyGoblinRanged";a.MaxHp=30;a.PhysicalDamage=5;a.SpeedInCellsPerSecond=1;a.RangeInCells=3;a.AttackIntervalSeconds=1.5f;a.ProjectileId="projectile.enemy";a.Enabled=true;a.CollisionRadiusInCells=.35f;a.DropTableId="drop.gold";});
            BossSkillDefinitionSO shot=Asset<BossSkillDefinitionSO>("P4","BossShot",delegate(BossSkillDefinitionSO a){a.Id="boss.shot";a.PeriodSeconds=5;a.FirstDelaySeconds=5;a.ProjectileId="projectile.enemy";a.ProjectileDamage=15;a.Effects=new EnemyEffectConfig[0];a.Summons=new SpawnGroupConfig[0];});
            BossSkillDefinitionSO summon=Asset<BossSkillDefinitionSO>("P4","BossSummon",delegate(BossSkillDefinitionSO a){a.Id="boss.summon";a.PeriodSeconds=20;a.FirstDelaySeconds=20;a.Effects=new EnemyEffectConfig[0];a.Summons=new SpawnGroupConfig[] {Group("summon","enemy.goblin.melee",3,false)};});
            BossSkillDefinitionSO curse=Asset<BossSkillDefinitionSO>("P4","BossCurse",delegate(BossSkillDefinitionSO a){a.Id="boss.curse";a.PeriodSeconds=40;a.FirstDelaySeconds=40;a.Summons=new SpawnGroupConfig[0];a.Effects=new EnemyEffectConfig[] {new EnemyEffectConfig {Id="curse",Kind=EffectKind.Damage,TargetPolicy=EffectTargetPolicy.Spring,PhysicalDamage=10}};});
            EnemyDefinitionSO boss=Asset<EnemyDefinitionSO>("P4","SoulKing",delegate(EnemyDefinitionSO a){a.Id="enemy.soulking";a.PrefabKey="EnemySoulKing";a.MaxHp=1000;a.PhysicalDamage=20;a.SpeedInCellsPerSecond=.5f;a.RangeInCells=1;a.AttackIntervalSeconds=2;a.Enabled=true;a.IsFinalBoss=true;a.CollisionRadiusInCells=.35f;a.DropTableId="drop.gold";a.BossSkillIds=new string[] {"boss.shot","boss.summon","boss.curse"};});
            EnemyDefinitionSO wraith=Asset<EnemyDefinitionSO>("P4","WraithDeferred",delegate(EnemyDefinitionSO a){a.Id="enemy.wraith";a.PrefabKey="EnemyWraith";a.Enabled=false;a.IsFinalBoss=false;a.MaxHp=30;a.PhysicalDamage=0;a.SpeedInCellsPerSecond=1;a.RangeInCells=1;a.AttackIntervalSeconds=1;a.CollisionRadiusInCells=.35f;});
            List<WaveDefinitionSO> waves=new List<WaveDefinitionSO>();for(int day=1;day<=7;day++){int d=day;waves.Add(Asset<WaveDefinitionSO>("P4","StoryNight"+d,delegate(WaveDefinitionSO a){a.Id="wave.story."+d;a.DayIndex=d;a.WaveIndex=1;List<SpawnGroupConfig> groups=new List<SpawnGroupConfig>();groups.Add(Group("melee","enemy.goblin.melee",2+d,true));groups.Add(Group("ranged","enemy.goblin.ranged",d>1?2:1,true));groups[1].StartDelaySeconds=4;if(d==7){SpawnGroupConfig final=Group("finalboss","enemy.soulking",1,false);final.StartDelaySeconds=8;groups.Add(final);}a.Groups=groups.ToArray();}));}
            WaveDefinitionSO template=Asset<WaveDefinitionSO>("P4","EndlessWaveTemplate",delegate(WaveDefinitionSO a){a.Id="wave.endless.template";a.DayIndex=1;a.WaveIndex=1;a.Groups=new SpawnGroupConfig[] {Group("melee","enemy.goblin.melee",5,true),Group("ranged","enemy.goblin.ranged",2,true)};});
            EndlessConfigSO endless=Asset<EndlessConfigSO>("P4","EndlessConfig",delegate(EndlessConfigSO a){a.Id="endless.default";a.WaveTemplateIds=new string[] {"wave.endless.template"};a.HpGrowthPerNight=.25f;a.DamageGrowthPerNight=.25f;a.MilestoneNight=50;a.MilestoneRewards=new EnemyResourceConfig[] {new EnemyResourceConfig {Bucket=ResourceBucket.Inventory,DefinitionId="res.gold",Amount=100}};});
            ItemDefinitionSO heal=Asset<ItemDefinitionSO>("P5","HealItem",delegate(ItemDefinitionSO a){a.Id="item.heal";a.DisplayNameKey="灵泉治疗";a.AllowedPhases=new GamePhase[] {GamePhase.Build,GamePhase.Night};a.Effects=new GameEffectConfig[] {new GameEffectConfig {Id="heal",Kind=EffectKind.Heal,TargetPolicy=EffectTargetPolicy.Spring,Amount=25}};});
            ItemDefinitionSO stack=Asset<ItemDefinitionSO>("P5","TowerStackItem",delegate(ItemDefinitionSO a){a.Id="item.tower_stack";a.DisplayNameKey="塔强化";a.AllowedPhases=new GamePhase[] {GamePhase.Build,GamePhase.Night};a.Effects=new GameEffectConfig[] {new GameEffectConfig {Id="stack",Kind=EffectKind.AddTowerStacks,TargetPolicy=EffectTargetPolicy.SelectedActor,Stacks=1}};});
            DayEventDefinitionSO dayEvent=Asset<DayEventDefinitionSO>("P5","QuietMorning",delegate(DayEventDefinitionSO a){a.Id="event.quiet_morning";a.MinDayInclusive=1;a.MaxDayInclusive=int.MaxValue;a.Weight=1;a.Text="清晨的灵泉平静如常。";a.Modes=new GameMode[] {GameMode.Story,GameMode.Endless};a.Options=new EventOptionConfig[] {new EventOptionConfig {Id="continue",Text="开始今天的部署",Cost=new Spotlight.Gameplay.Definitions.ResourceAmountConfig[0],Effects=new GameEffectConfig[0]}};});
            LevelConfigSO level=Asset<LevelConfigSO>("P1","LevelPrototype",delegate(LevelConfigSO a){a.Id="level.prototype";a.Columns=9;a.Rows=6;a.CellSize=1;a.Origin=new WorldPointConfig {X=-4.5f,Y=-3};a.SpringCell=new CellCoordConfig {X=8,Y=2};a.SpawnPoints=new SpawnPointConfig[] {new SpawnPointConfig {Id="spawn.ground",Cell=new CellCoordConfig {X=0,Y=2}}};CellCoordConfig[] cells=new CellCoordConfig[9];for(int x=0;x<9;x++)cells[x]=new CellCoordConfig {X=x,Y=2};a.Paths=new PathConfig[] {new PathConfig {Id="path.ground",Movement=MovementKind.Ground,SpawnPointId="spawn.ground",Cells=cells}};List<Spotlight.Core.Data.ResourceAmountConfig> amounts=new List<Spotlight.Core.Data.ResourceAmountConfig>();foreach(string id in ids)amounts.Add(new Spotlight.Core.Data.ResourceAmountConfig {Bucket=ResourceBucket.Hand,DefinitionId="elm."+id,Amount=3});amounts.Add(new Spotlight.Core.Data.ResourceAmountConfig {Bucket=ResourceBucket.Inventory,DefinitionId="res.gold",Amount=100});amounts.Add(new Spotlight.Core.Data.ResourceAmountConfig {Bucket=ResourceBucket.Inventory,DefinitionId="item.heal",Amount=1});amounts.Add(new Spotlight.Core.Data.ResourceAmountConfig {Bucket=ResourceBucket.Inventory,DefinitionId="item.tower_stack",Amount=1});a.InitialResources=amounts.ToArray();});
            PrototypeCatalogSO catalog=Asset<PrototypeCatalogSO>("P1","PrototypeCatalog",delegate(PrototypeCatalogSO a){a.Settings=settings;a.Levels=new LevelConfigSO[] {level};a.Resources=new ResourceDefinitionSO[] {gold};a.Elements=elements.ToArray();a.ProductionRules=production.ToArray();a.Reactions=reactions.ToArray();a.Statuses=statuses.ToArray();a.Gauges=gauges.ToArray();a.Towers=new TowerDefinitionSO[] {tower};a.Projectiles=new ProjectileDefinitionSO[] {projectile,enemyProjectile};a.Skills=new SpecialSkillDefinitionSO[] {skill};a.Enemies=new EnemyDefinitionSO[] {melee,ranged,boss,wraith};a.BossSkills=new BossSkillDefinitionSO[] {shot,summon,curse};a.DropTables=new DropTableSO[] {drop};a.StoryWaves=waves.ToArray();a.WaveTemplates=new WaveDefinitionSO[] {template};a.Endless=endless;a.Items=new ItemDefinitionSO[] {heal,stack};a.DayEvents=new DayEventDefinitionSO[] {dayEvent};a.PrefabEntries=prefabs??new PrefabEntry[0];});
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();return catalog;
        }
    }
}
#endif

