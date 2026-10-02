using System;
using System.Collections.Generic;
using Spotlight.Contracts;
using UnityEngine;
using UnityEngine.UI;
namespace Spotlight.Presentation
{
    /// <summary>Drag Text fields in the Inspector; the services remain the sole source of gameplay state.</summary>
    public sealed class ReadonlyHudView : MonoBehaviour,IGameView
    {
        public Text SpringHpText;
        public Text GoldText;
        [Tooltip("依次拖入水、火、土、木、风、雷的六个文字。")]
        public Text[] HandTexts=new Text[6];
        private GameViewContext context;
        private readonly List<IDisposable> subscriptions=new List<IDisposable>();
        private readonly string[] ids={"elm.water","elm.fire","elm.earth","elm.wood","elm.wind","elm.thunder"};
        private readonly string[] names={"水","火","土","木","风","雷"};
        public void Bind(GameViewContext value)
        {
            if(value==null)throw new ArgumentNullException("value");Unbind();context=value;
            subscriptions.Add(value.Events.Subscribe<HealthChangedEvent>(delegate(HealthChangedEvent e){RefreshAll();}));
            subscriptions.Add(value.Events.Subscribe<ResourcesChangedEvent>(delegate(ResourcesChangedEvent e){RefreshAll();}));
            subscriptions.Add(value.Events.Subscribe<PhaseChangedEvent>(delegate(PhaseChangedEvent e){RefreshAll();}));
            RefreshAll();
        }
        public void RefreshAll()
        {
            if(context==null)return;
            ActorSnapshot spring;
            if(SpringHpText!=null)SpringHpText.text=context.World.TryGetActor(context.World.SpringId,out spring)?"灵泉 "+Mathf.CeilToInt(spring.CurrentHp)+" / "+Mathf.CeilToInt(spring.MaxHp):"守护灵泉";
            if(GoldText!=null)GoldText.text="金币 "+context.Resources.GetQuantity(ResourceBucket.Inventory,"res.gold");
            for(int i=0;HandTexts!=null&&i<HandTexts.Length&&i<ids.Length;i++)if(HandTexts[i]!=null)HandTexts[i].text=names[i]+" × "+context.Resources.GetQuantity(ResourceBucket.Hand,ids[i]);
        }
        public void Unbind(){foreach(IDisposable subscription in subscriptions)subscription.Dispose();subscriptions.Clear();context=null;}
        private void OnDestroy(){Unbind();}
    }
}
