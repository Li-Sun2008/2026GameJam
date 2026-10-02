using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
namespace Spotlight.Elements.Definitions
{
    [CreateAssetMenu(menuName="Spotlight/P2/ProductionRule",fileName="ProductionRule")]
    public sealed class ProductionRuleSO : ScriptableObject
    {
        public string Id = "production.water";
        public bool Enabled = true;
        public ProductionMatch Match = ProductionMatch.SingleBlock;
        public ElementType A = ElementType.Water;
        public ElementType B;
        public bool ConsumeA;
        public bool ConsumeB;
        public string OutputElementId = "element.water";
        public int OutputCount = 1;
        public int Priority;
        public bool RandomTieBreak;
        public ProductionRuleDefinition ToDefinition()
        {
            return new ProductionRuleDefinition { Id=Id,Enabled=Enabled,Match=Match,A=A,B=B,ConsumeA=ConsumeA,ConsumeB=ConsumeB,OutputElementId=OutputElementId,OutputCount=OutputCount,Priority=Priority,RandomTieBreak=RandomTieBreak };
        }
    }
}
