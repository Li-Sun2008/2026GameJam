// Generated verbatim from Docs/Contracts/Spotlight.Contracts.cs (2.1.0).
using System;
using System.Collections.Generic;

namespace Spotlight.Contracts
{
    public interface IGameCatalog
    {
        GameSettings GetSettings();
        bool TryGetLevel(string id, out LevelDefinition value);
        bool TryGetElement(string id, out ElementDefinition value);
        bool TryGetTower(string id, out TowerDefinition value);
        bool TryGetProjectile(string id, out ProjectileDefinition value);
        bool TryGetStatus(string id, out StatusDefinition value);
        bool TryGetEnemy(string id, out EnemyDefinition value);
        bool TryGetItem(string id, out ItemDefinition value);
        bool TryGetResource(string id, out ResourceDefinition value);
        bool TryGetEvent(string id, out DayEventDefinition value);
        bool TryGetBossSkill(string id, out BossSkillDefinition value);
        bool TryGetDropTable(string id, out DropTableDefinition value);
        bool TryGetSkill(string id, out SpecialSkillDefinition value);
        EndlessDefinition GetEndlessDefinition();
        IReadOnlyList<ProductionRuleDefinition> GetProductionRules();
        IReadOnlyList<ReactionDefinition> GetReactionRules(ReactionContext context);
        IReadOnlyList<GaugeDefinition> GetGaugeDefinitions();
        IReadOnlyList<DayEventDefinition> GetDayEvents();
        IReadOnlyList<WaveDefinition> GetWaves(int dayIndex, GameMode mode);
        IReadOnlyList<string> ValidateAll();
    }
}

