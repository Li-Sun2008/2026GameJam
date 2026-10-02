using Spotlight.Contracts;
namespace Spotlight.Core.Data
{
    public sealed class CatalogData
    {
        public GameSettings Settings=new GameSettings();
        public EndlessDefinition Endless;
        public GaugeDefinition[] Gauges=new GaugeDefinition[0];
        public LevelDefinition[] Levels=new LevelDefinition[0];
        public ElementDefinition[] Elements=new ElementDefinition[0];
        public TowerDefinition[] Towers=new TowerDefinition[0];
        public ProjectileDefinition[] Projectiles=new ProjectileDefinition[0];
        public StatusDefinition[] Statuses=new StatusDefinition[0];
        public EnemyDefinition[] Enemies=new EnemyDefinition[0];
        public ItemDefinition[] Items=new ItemDefinition[0];
        public ResourceDefinition[] Resources=new ResourceDefinition[0];
        public DayEventDefinition[] DayEvents=new DayEventDefinition[0];
        public BossSkillDefinition[] BossSkills=new BossSkillDefinition[0];
        public DropTableDefinition[] DropTables=new DropTableDefinition[0];
        public SpecialSkillDefinition[] Skills=new SpecialSkillDefinition[0];
        public ProductionRuleDefinition[] ProductionRules=new ProductionRuleDefinition[0];
        public ReactionDefinition[] Reactions=new ReactionDefinition[0];
        public WaveDefinition[] StoryWaves=new WaveDefinition[0];
        public WaveDefinition[] WaveTemplates=new WaveDefinition[0];
    }
}
