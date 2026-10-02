using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Core.Data
{
    [CreateAssetMenu(menuName="Spotlight/P1/GameConfig",fileName="GameConfig")]
    public sealed class GameConfigSO : ScriptableObject
    {
        public string ConfigVersion = "prototype.1";
        public int StoryDays = 7;
        public int LogicTicksPerGameSecond = 30;
        public float SpringMaxHp = 100;
        public float SpringCollisionRadiusInCells = 0.35f;
        public int MaxReactionActionsPerTick = 256;
        public string SpecialSkillId = "skill.spotlight";
        public string TutorialText = "放置元素和塔，然后开始夜晚。";
        public GameSettings ToSettings()
        {
            return new GameSettings { ConfigVersion=ConfigVersion,StoryDays=StoryDays,LogicTicksPerGameSecond=LogicTicksPerGameSecond,SpringMaxHp=SpringMaxHp,SpringCollisionRadiusInCells=SpringCollisionRadiusInCells,MaxReactionActionsPerTick=MaxReactionActionsPerTick,SpecialSkillId=SpecialSkillId,TutorialText=TutorialText };
        }
    }
}
